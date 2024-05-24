using System;

namespace Mazing;

public sealed class Player : Component, Component.INetworkSpawn
{
	[RequireComponent] public Mazer Mazer { get; set; } = null!;
	[RequireComponent] public Holder Holder { get; set; } = null!;
	[RequireComponent] public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	public bool IsDead => _isDeadLocal || Mazer.State == MazerState.Dead;

	[Property]
	public TimeSince DeathTime { get; set; }

	[Property, Sync]
	public bool HasExited { get; set; }

	[Property, Sync]
	public string? ClothingJson { get; set; }

	[Property] public event Action? Exiting;

	private bool _wasExiting;
	private bool _isDeadLocal;

	private GameObject? _ragdoll;
	private ClothingContainer? _clothing;

	protected override void OnStart()
	{
		Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost && Network.OwnerId.Equals( Guid.Empty ) && !IsDead )
		{
			Kill( Vector3.Up * 300f );
			return;
		}

		if ( !_wasExiting && Mazer.Throwable.IsExiting )
		{
			_wasExiting = Mazer.Throwable.IsExiting;

			Exiting?.Invoke();
		}

		if ( IsProxy )
		{
			return;
		}

		Mazer.MoveSpeed = 120f * 8f / (8f + Holder.HeldItems.Count);

		if ( Mazer.State != MazerState.Falling || !Mazer.Throwable.IsExiting )
		{
			return;
		}

		if ( Transform.Position.z < -512f )
		{
			HasExited = true;
		}
	}

	protected override void OnDestroy()
	{
		_ragdoll?.Destroy();
		_ragdoll = null;
	}

	[Broadcast( NetPermission.HostOnly )]
	public void Respawn( Vector3 pos )
	{
		_ragdoll?.Destroy();
		_ragdoll = null;

		Transform.Position = pos;

		Mazer.State = MazerState.Falling;

		_wasExiting = false;
		_isDeadLocal = false;

		Mazer.Throwable.IsAirborne = false;
		Mazer.Throwable.IsExiting = false;
		Mazer.Throwable.CanExit = true;

		Mazer.CharacterController.Velocity = Vector3.Zero;

		Holder.Enabled = true;

		HasExited = false;

		Tags.Add( "player" );
		Tags.Remove( "ghost" );
	}

	public void Kill( Vector3 force, bool ragdoll = true )
	{
		if ( _isDeadLocal || IsDead )
		{
			return;
		}

		_isDeadLocal = true;
		DeathTime = 0f;

		KillInternal( force, ragdoll );
	}

	[Authority( NetPermission.HostOnly )]
	private void KillInternal( Vector3 force, bool ragdoll )
	{
		if ( Mazer.State == MazerState.Dead )
		{
			return;
		}

		Sandbox.Services.Stats.Increment( "deaths", 1 );

		DeathTime = 0f;

		Mazer.State = MazerState.Dead;
		Mazer.Throwable.CanExit = false;

		Tags.Remove( "player" );
		Tags.Add( "ghost" );

		if ( ragdoll )
		{
			SpawnRagdoll( force );
		}

		var dir = force.WithZ( 0f ).IsNearZeroLength
			? (Direction)Random.Shared.Next( 0, 4 )
			: ((Vector2)force).GetDirection();

		Holder.ThrowAll( dir, 1 );
		Holder.Enabled = false;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void SpawnRagdoll( Vector3 force )
	{
		_ragdoll?.Destroy();
		_ragdoll = null;

		_ragdoll = new GameObject( true, $"{GameObject.Name} (Ragdoll)" )
		{
			Transform = { World = Transform.World },
			Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
		};

		var renderer = _ragdoll.Components.Create<SkinnedModelRenderer>();

		renderer.Model = ModelRenderer.Model;
		renderer.SceneModel.UseAnimGraph = false;

		renderer.BoneMergeTarget = ModelRenderer;
		renderer.BoneMergeTarget = null;

		if ( ClothingJson is not null )
		{
			_clothing ??= ClothingContainer.CreateFromJson( ClothingJson );
			_clothing.Apply( renderer );
		}

		var physics = _ragdoll.Components.Create<ModelPhysics>();

		physics.Model = renderer.Model;
		physics.Renderer = renderer;

		_ragdoll.Tags.Add( "ragdoll" );

		if ( force.IsNearZeroLength )
		{
			return;
		}

		var upperBody = physics.PhysicsGroup.GetBody( "spine_2" );
		var hitPos2d = Random.Shared.VectorInCircle( 12f );
		var normal = force.Normal;
		var up = Vector3.Up;
		var right = Vector3.Cross( normal, up ).Normal;

		upperBody.ApplyImpulseAt( Transform.Position + right * hitPos2d.x + up * (hitPos2d.y + 48f), force );
	}

	public void OnNetworkSpawn( Connection owner )
	{
		ClothingJson = owner.GetUserData( "avatar" );

		_clothing = ClothingContainer.CreateFromJson( ClothingJson );
		_clothing.Apply( ModelRenderer );
	}
}
