using System;

namespace Mazing;

public class Charger : Wanderer
{
	private bool _isCharging;
	private Direction _chargeDirection;
	private Player? _chaseTarget;
	private (int Row, int Col) _lastTargetPos;
	private TimeUntil _stunned;

	[Property] public bool Chase { get; set; }

	[Property] public float StunTime { get; set; } = 1.5f;

	[Property] public event Action? StartedCharging;
	[Property] public event Action? StoppedCharging;

	protected override Direction? GetNewTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		if ( _isCharging )
		{
			if ( Chase )
			{
				if ( _lastTargetPos == (row, col) )
				{
					StopCharging( MazeObject.View[row, col, Direction] != WallState.Open );
					return null;
				}

				var path = Mazer.MazeObject.View.FindPath( (row, col), _lastTargetPos, 256,
					Mazer.MazeObject.Maze.GetPathCost );

				if ( path is null )
				{
					return base.GetNewTarget();
				}

				return path[0].GetDirectionTo( path[1] );
			}
			else if ( !IsValidDirection( _chargeDirection ) )
			{
				StopCharging( true );
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
				if ( !IsValidDirection( row, col, Direction ) )
				{
					break;
				}

				(row, col) = Direction.GetNeighbor( row, col );

				var target = MazeObject.Maze.GetObjectsInCell( row, col )
					.Select( x => x.Components.Get<Player>() )
					.FirstOrDefault( x => x != null );

				if ( target is not null )
				{
					_chargeDirection = Direction;
					_chaseTarget = Chase ? target : null;
					_lastTargetPos = (row, col);

					StartCharging();
				}
			}
		}
		else if ( _chaseTarget is not null )
		{
			if ( _chaseTarget.Mazer.State != MazerState.Walking )
			{
				_chaseTarget = null;
			}
			else
			{
				_lastTargetPos = _chaseTarget.Mazer.MazeObject.CellIndex;
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
	public void StopCharging( bool wallHit )
	{
		_isCharging = false;
		_stunned = StunTime;

		StoppedCharging?.Invoke();
	}
}
