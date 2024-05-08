
namespace Mazing;

public sealed class Holdable : Component, Component.ITriggerListener
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Throwable Throwable { get; set; } = null!;

	[Property] public bool IsHeavy { get; set; }

	[Property] public bool PickupOnTouch { get; set; } = true;
	[Property] public bool PickupOnLand { get; set; } = true;

	[Property] public Holder? Holder { get; set; }

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !PickupOnTouch || Throwable.Enabled || Holder is not null )
		{
			return;
		}

		if ( other.Components.Get<Holder>() is not {} holder )
		{
			return;
		}

		holder.TryPickUp( this );
	}
}

