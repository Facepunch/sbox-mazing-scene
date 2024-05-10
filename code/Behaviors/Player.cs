using System;
using Sandbox.Physics;

namespace Mazing;

public sealed class Player : Component
{
	[RequireComponent] public Mazer Mazer { get; set; } = null!;
	[RequireComponent] public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[Property, Sync]
	public bool IsExiting { get; set; }

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
		if ( !_wasExiting && IsExiting )
		{
			_wasExiting = IsExiting;

			Exiting?.Invoke();
		}

		if ( IsProxy )
		{
			return;
		}

		if ( Mazer.State != MazerState.Falling )
		{
			return;
		}

		if ( !(Transform.Position.z < -64f) )
		{
			return;
		}

		IsExiting = true;

		if ( Transform.Position.z < -512f )
		{
			HasExited = true;
		}
	}

	[Authority( NetPermission.HostOnly )]
	public void Respawn( Vector3 pos )
	{
		Transform.Position = pos;

		Mazer.State = MazerState.Falling;

		_wasExiting = false;

		IsExiting = false;
		HasExited = false;

		Tags.Add( "player" );
		Tags.Remove( "ghost" );

		UpdateClothing();
	}

	[Authority( NetPermission.HostOnly )]
	public void Kill( Vector3 force )
	{
		Mazer.State = MazerState.Dead;

		Tags.Remove( "player" );
		Tags.Add( "ghost" );

		SpawnRagdoll( force );

		if ( Components.Get<Holder>( FindMode.Enabled | FindMode.InSelf ) is { } holder )
		{
			holder.Drop();
		}

		if ( Components.Get<Holdable>( FindMode.Enabled | FindMode.InSelf ) is { } holdable )
		{
			holdable.Holder?.Drop();
		}
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
