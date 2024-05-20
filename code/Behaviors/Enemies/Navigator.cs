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
	public float MinTargetUpdatePeriod { get; set; } = 1f;

	private (int Row, int Col)? _target = null;

	private TimeUntil _nextTargetUpdate;

	protected Direction Direction { get; private set; }

	protected override void OnStart()
	{
		_nextTargetUpdate = 3f + Random.Shared.NextSingle();

		Direction = ((Vector2)Transform.Rotation.Forward).GetDirection();
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
			if ( GetNewTarget() is { } targetDir )
			{
				_target = targetDir.GetNeighbor( row, col );
				Direction = targetDir;
			}

			_nextTargetUpdate = MinTargetUpdatePeriod;
		}

		if ( _target is not {} target )
		{
			return;
		}

		var dist = Math.Abs( target.Row - row ) + Math.Abs( target.Col - col );
		var targetPos = MazeObject.Maze.MazeToWorldPos( target.Row, target.Col );
		var diff = (Vector2)(targetPos - Transform.Position);

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
