using System;

namespace Mazing;

public sealed class Shooter : Component
{
	[RequireComponent]
	public Mazer Mazer { get; set; } = null!;

	[Property]
	public GameObject ProjectilePrefab { get; set; } = null!;

	[Property]
	public event Action? Shot;

	public void Shoot()
	{
		var go = ProjectilePrefab.Clone( Transform.Position.WithZ( 32f ) );
		var proj = go.Components.Get<Projectile>();

		proj.Fire( Mazer.Direction );

		go.NetworkSpawn();

		DispatchShot();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchShot()
	{
		Shot?.Invoke();
	}
}
