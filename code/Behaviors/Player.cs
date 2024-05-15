using System;
using System.Diagnostics.CodeAnalysis;
using Sandbox.Physics;

namespace Mazing;

public sealed class Player : Component
{
	[RequireComponent] public Mazer Mazer { get; set; } = null!;
	[RequireComponent] public Holder Holder { get; set; } = null!;
	[RequireComponent] public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[Property, Sync]
	public bool IsDead { get; set; }

	[Property, Sync]
	public TimeSince DeathTime { get; set; }

	[Property, Sync]
	public bool HasExited { get; set; }

	[Property] public event Action? Exiting;

	private bool _wasExiting;

	private GameObject? _ragdoll;

	[Authority]
	public void UpdateClothing()
	{
		Mazer.SetClothing( ClothingContainer.CreateFromLocalUser() );
	}

	protected override void OnUpdate()
	{
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

	[Authority( NetPermission.HostOnly )]
	public void Respawn( Vector3 pos )
	{
		RemoveRagdoll();

		Transform.Position = pos;

		Mazer.State = MazerState.Falling;

		_wasExiting = false;

		Mazer.Throwable.Enabled = false;
		Mazer.Throwable.IsExiting = false;
		Mazer.Throwable.CanExit = true;

		Mazer.CharacterController.Velocity = Vector3.Zero;

		Holder.Enabled = true;

		HasExited = false;

		IsDead = false;

		Tags.Add( "player" );
		Tags.Remove( "ghost" );

		UpdateClothing();
	}

	[Authority( NetPermission.HostOnly )]
	public void Kill( Vector3 force, bool ragdoll = true )
	{
		if ( IsDead )
		{
			return;
		}

		IsDead = true;
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
			? (Direction)Random.Shared.Int( 0, 4 )
			: ((Vector2)force).GetDirection();

		Holder.ThrowAll( dir, 1 );
		Holder.Enabled = false;
	}

	[Broadcast]
	public void RemoveRagdoll()
	{
		_ragdoll?.Destroy();
		_ragdoll = null;
	}

	[Broadcast]
	private void SpawnRagdoll( Vector3 force )
	{
		RemoveRagdoll();

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

		var clothing = ClothingContainer.CreateFromJson( Mazer.ClothingJson ?? "{}" );

		clothing.Apply( renderer );

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
}
