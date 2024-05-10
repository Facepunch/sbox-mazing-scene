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
		}

		if ( IsProxy ) return;

		var (row, col) = MazeObject.CellIndex;

		var objects = MazeObject.GetObjectsInSameCell().ToArray();

		foreach ( var mazeObject in objects )
		{
			if ( !IsOpen && mazeObject.Components.Get<Key>() is { } key )
			{
				if ( objects.Any( x => x.Components.Get<Holder>() is not null ) )
				{
					// Key is about to be picked up!

					continue;
				}

				Open( key );
				continue;
			}

			if ( IsOpen && mazeObject.Components.Get<Throwable>( true ) is { IsExiting: false, CanExit: true } throwable )
			{
				throwable.Throw( row, col, Direction.North, 0 );
				continue;
			}
		}
	}
}
