using System;
using Sandbox;

namespace Mazing;

public sealed class Wander : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Mazer Mazer { get; set; } = null!;

	private Dictionary<(int, int), TimeSince> _lastVisited = new();
	private (int Row, int Col) _target;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		var (row, col) = MazeObject.CellIndex;
		var dist = Math.Abs( _target.Row - row ) + Math.Abs( _target.Col - col );
		var targetPos = MazeObject.Maze.MazeToWorldPos( _target.Row, _target.Col );
		var diff = (Vector2)(targetPos - Transform.Position);

		if ( dist > 1 || diff.LengthSquared < 4f )
		{
			UpdateTarget();
		}

		Mazer.MoveInput = diff.Normal;
	}

	private static float ScoreDirection( Direction dir, Direction curDir, Direction preferredDir )
	{
		if ( dir == preferredDir )
		{
			return 0f;
		}

		if ( dir == curDir )
		{
			return 1f;
		}

		if ( dir == curDir.Opposite() )
		{
			return 3f;
		}

		return Random.Shared.Float( 1.5f, 2.5f );
	}

	private TimeSince LastVisited( (int row, int col) cell )
	{
		return _lastVisited.TryGetValue( cell, out var since )
			? since : 60f;
	}

	public void UpdateTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		_lastVisited[(row, col)] = 0f;

		var dir = Helpers.Directions
			.Where( x => MazeObject.View[row, col, x] == WallState.Open )
			.MaxBy( x => LastVisited( x.GetNeighbor( row, col ) ) + Random.Shared.NextSingle() );

		_target = dir.GetNeighbor( row, col );
	}
}
