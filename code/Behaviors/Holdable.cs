
using System;

namespace Mazing;

public sealed class Holdable : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Throwable Throwable { get; set; } = null!;

	[Property] public Holder? Holder { get; set; }

	[Property]
	public event Action? PickedUp;

	[Property]
	public event Action? Dropped;

	[Property]
	public Vector3 HeldVelocity { get; set; }

	[Property]
	public TimeSince HeldTime { get; set; }

	protected override void OnDisabled()
	{
		Holder?.Drop();
	}

	protected override void OnDestroy()
	{
		Holder?.Drop();
	}

	[Broadcast]
	public void DispatchPickedUp()
	{
		PickedUp?.Invoke();
	}

	[Broadcast]
	public void DispatchDropped()
	{
		Dropped?.Invoke();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || Holder is null )
		{
			return;
		}

		var index = Holder.HeldItems.IndexOf( this );

		if ( index < 0 )
		{
			return;
		}

		Transform.LocalPosition = Vector3.Lerp( Transform.LocalPosition, Vector3.Up * (64f + index * 16f), Helpers.Ease( 0.125f ) );
	}
}
