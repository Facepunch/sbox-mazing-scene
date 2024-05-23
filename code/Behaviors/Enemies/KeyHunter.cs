using System;

namespace Mazing;

public class KeyHunter : Wanderer
{
	[Property] public event Action? StartedHunting;
	[Property] public event Action? StoppedHunting;

	[Property, Sync]
	public bool IsHunting { get; set; }

	[Property]
	public bool IsTracker { get; set; }

	[Property]
	public TimeSince StateChangeTime { get; set; }

	private Exit? _exit;
	private Key? _key;
	private Player? _target;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			base.OnUpdate();
			return;
		}

		_exit ??= Scene.Components.Get<Exit>( FindMode.Enabled | FindMode.InChildren );

		if ( _exit.IsOpen )
		{
			_key = null;

			if ( !IsHunting )
			{
				StartHunting();
			}
		}
		else
		{
			_key ??= Scene.Components.Get<Key>( FindMode.Enabled | FindMode.InChildren );

			if ( _key?.Holdable.Holder is { } holder && holder.Components.Get<Player>() is {} target )
			{
				if ( !IsHunting || IsTracker && target != _target )
				{
					_target = target;
					StartHunting();
				}
			}
			else if ( IsHunting && (!IsTracker || _target?.IsDead is true) )
			{
				_target = null;
				StopHunting();
			}
		}

		base.OnUpdate();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void StartHunting()
	{
		ClearTarget();

		IsHunting = true;
		StartedHunting?.Invoke();

		StateChangeTime = 0f;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void StopHunting()
	{
		IsHunting = false;
		StoppedHunting?.Invoke();

		StateChangeTime = 0f;
	}

	protected override Direction? GetNewTarget()
	{
		if ( !IsHunting || _target is null )
		{
			return base.GetNewTarget();
		}

		var target = _target.Mazer.MazeObject.CellIndex;
		var path = MazeObject.View.FindPath( MazeObject.CellIndex, target, 256, MazeObject.Maze.GetPathCost );

		return path?[0].GetDirectionTo( path[1] ) ?? base.GetNewTarget();
	}
}
