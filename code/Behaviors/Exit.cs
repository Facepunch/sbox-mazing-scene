using System;
using Sandbox;

namespace Mazing;

public sealed class Exit : Component
{
	[RequireComponent]
	public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[RequireComponent]
	public MazeObject MazeObject { get; set; } = null!;

	[RequireComponent]
	public Collider Collider { get; set; } = null!;

	[Property]
	public Collider Trigger { get; set; } = null!;

	[Property, Sync]
	public bool IsOpen { get; set; }

	private bool _wasOpen;

	[Property]
	public event Action? Opened;

	public void Open( Key key )
	{
		IsOpen = true;

		key.GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		if ( IsOpen && !_wasOpen )
		{
			_wasOpen = IsOpen;

			Opened?.Invoke();
			return;
		}

		if ( IsOpen )
		{
			return;
		}

		if ( IsProxy ) return;

		foreach ( var collider in Trigger.Touching )
		{
			if ( collider.Components.Get<Key>() is { } key )
			{
				Open( key );
				continue;
			}

			if ( collider.Components.Get<Holder>() is { HeldItem: {} heldItem } )
			{
				if ( heldItem.Components.Get<Key>() is { } heldKey )
				{
					Open( heldKey );
					continue;
				}
			}
		}
	}
}
