using System;

namespace Mazing;

public abstract class Navigator : Component
{
	private static int NextOrdinal { get; set; }

	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Mazer Mazer { get; set; } = null!;

	private Dictionary<(int, int), TimeSince> _lastVisited = new();
	private int _ordinal = ++NextOrdinal;

	protected TimeSince LastVisited( (int row, int col) cell )
	{
		return _lastVisited.TryGetValue( cell, out var since )
			? since : 60f;
	}

	[Property]
	public float MinTargetUpdatePeriod { get; set; } = 0.5f;

	private (int Row, int Col)? _target = null;

	private TimeUntil _nextTargetUpdate;
	private bool _justSpawned = true;

	protected Direction Direction { get; private set; }

	protected IEnumerable<Direction> ValidDirections => Helpers.Directions.Where( IsValidDirection );

	protected bool IsValidDirection( int row, int col, Direction dir )
	{
		if ( MazeObject.View[row, col, dir] != WallState.Open )
		{
			return false;
		}

		(row, col) = dir.GetNeighbor( row, col );

		return !MazeObject.Maze
			.GetObjectsInCell( row, col )
			.Any( x => x.Components.Get<Exit>() is { IsOpen: true } );
	}

	protected bool IsValidDirection( Direction dir )
	{
		var (row, col) = MazeObject.CellIndex;
		return IsValidDirection( row, col, dir );
	}

	protected override void OnStart()
	{
		_nextTargetUpdate = 3f + Random.Shared.NextSingle();

		Direction = ((Vector2)WorldRotation.Forward).GetDirection();
	}

	public void ClearTarget()
	{
		_target = null;
		_nextTargetUpdate = 0f;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		var (row, col) = MazeObject.CellIndex;

		_lastVisited[(row, col)] = 0f;

		if ( _target is null && _nextTargetUpdate <= 0f )
		{
			if ( _justSpawned && IsValidDirection( Direction ) )
			{
				_target = Direction.GetNeighbor( row, col );
			}
			else if ( GetNewTarget() is { } targetDir )
			{
				_target = targetDir.GetNeighbor( row, col );
				Direction = targetDir;
			}

			_nextTargetUpdate = MinTargetUpdatePeriod;
			_justSpawned = false;
		}

		if ( _target is not {} target )
		{
			return;
		}

		var dist = Math.Abs( target.Row - row ) + Math.Abs( target.Col - col );
		var targetPos = MazeObject.Maze.MazeToWorldPos( target.Row, target.Col );
		var diff = (Vector2)(targetPos - WorldPosition);

		var input = Direction.GetNormal();
		var otherNavigatorCount = MazeObject
			.GetObjectsInSameCell()
			.Count( x => x.GameObject != GameObject
				&& x.Components.Get<Navigator>() is { } navi
				&& (Math.Abs( navi.Mazer.MoveSpeed - Mazer.MoveSpeed ) < 1f)
				&& navi._ordinal < _ordinal );

		input *= 4f / (4f + otherNavigatorCount);

		Mazer.MoveInput = input;

		if ( Vector2.Dot( Direction.GetNormal(), diff ) <= 4f || dist > 1 )
		{
			_target = null;
		}
	}

	protected abstract Direction? GetNewTarget();
}
