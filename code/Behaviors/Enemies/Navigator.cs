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

	protected override void OnStart()
	{
		_nextTargetUpdate = 3f + Random.Shared.NextSingle();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( _target is null && _nextTargetUpdate <= 0f )
		{
			_target = GetNewTarget();
			_nextTargetUpdate = MinTargetUpdatePeriod;
		}

		if ( _target is not {} target )
		{
			return;
		}

		var (row, col) = MazeObject.CellIndex;
		var dist = Math.Abs( target.Row - row ) + Math.Abs( target.Col - col );
		var targetPos = MazeObject.Maze.MazeToWorldPos( target.Row, target.Col );
		var diff = (Vector2)(targetPos - Transform.Position);

		Mazer.MoveInput = diff.IsNearZeroLength ? 0f : diff.Normal;

		if ( diff.LengthSquared < 4f )
		{
			_target = null;
		}
	}

	protected abstract (int Row, int Col)? GetNewTarget();
}
