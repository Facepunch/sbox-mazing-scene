using System;

namespace Mazing;

public abstract class Navigator : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Mazer Mazer { get; set; } = null!;

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

		Mazer.MoveInput = Direction.GetNormal();

		if ( Vector2.Dot( Direction.GetNormal(), diff ) <= 4f || dist > 1 )
		{
			_target = null;
		}
	}

	protected abstract Direction? GetNewTarget();
}
