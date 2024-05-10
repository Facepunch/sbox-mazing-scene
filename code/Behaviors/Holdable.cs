
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
}
