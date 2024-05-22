using System;
using Sandbox;

namespace Mazing;

public sealed class SpikeTrap : Component
{
	[RequireComponent]
	public MazeObject MazeObject { get; set; } = null!;

	[Property]
	public TimeSince TriggerTime { get; set; }

	[Property] public float ResetTime { get; set; } = 3f;

	[Property] public event Action? Triggered;
	[Property] public event Action? Stabbed;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( TriggerTime < ResetTime )
		{
			return;
		}

		if ( MazeObject.GetObjectsInSameCell().Any( x => x.Components.Get<Holdable>() is not null ) )
		{
			Trigger();
		}
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void Trigger()
	{
		TriggerTime = 0f;

		Triggered?.Invoke();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void Stab()
	{
		Stabbed?.Invoke();
	}
}
