
using System;

namespace Mazing;

public sealed class Holdable : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Throwable Throwable { get; set; } = null!;

	[Property, Sync] public bool IsHeavy { get; set; }

	[Property, Sync] public bool PickupOnTouch { get; set; } = true;
	[Property, Sync] public bool PickupOnLand { get; set; } = true;

	[Property] public Holder? Holder { get; set; }

	[Property]
	public event Action? PickedUp;

	[Property]
	public event Action? Dropped;

	protected override void OnDisabled()
	{
		Holder?.Drop();
	}

	protected override void OnDestroy()
	{
		Holder?.Drop();
	}

	public void DispatchPickedUp()
	{
		PickedUp?.Invoke();
	}

	public void DispatchDropped()
	{
		Dropped?.Invoke();
	}
}
