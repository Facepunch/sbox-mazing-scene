using System;

namespace Mazing;

public class Charger : Wanderer
{
	private bool _isCharging;
	private Direction _chargeDirection;
	private TimeUntil _stunned;

	[Property] public float StunTime { get; set; } = 1.5f;

	[Property] public event Action? StartedCharging;
	[Property] public event Action? StoppedCharging;

	protected override Direction? GetNewTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		if ( _isCharging )
		{
			if ( MazeObject.View[row, col, _chargeDirection] != WallState.Open )
			{
				StopCharging();
				return null;
			}

			return _chargeDirection;
		}

		return _stunned > 0f ? null : base.GetNewTarget();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( !_isCharging )
		{
			var (row, col) = MazeObject.CellIndex;

			for ( var i = 1; i < 32; ++i )
			{
				if ( MazeObject.View[row, col, Direction] != WallState.Open )
				{
					break;
				}

				(row, col) = Direction.GetNeighbor( row, col );

				if ( MazeObject.Maze.GetObjectsInCell( row, col )
					.Any( x => x.Components.Get<Player>() is not null ) )
				{
					_chargeDirection = Direction;
					StartCharging();
				}
			}
		}

		base.OnUpdate();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void StartCharging()
	{
		_isCharging = true;

		StartedCharging?.Invoke();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void StopCharging()
	{
		_isCharging = false;
		_stunned = StunTime;

		StoppedCharging?.Invoke();
	}
}
