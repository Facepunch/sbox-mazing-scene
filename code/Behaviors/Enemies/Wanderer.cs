using System;

namespace Mazing;

public class Wanderer : Navigator
{
	private Dictionary<(int, int), TimeSince> _lastVisited = new();

	private TimeSince LastVisited( (int row, int col) cell )
	{
		return _lastVisited.TryGetValue( cell, out var since )
			? since : 60f;
	}

	protected override (int Row, int Col)? GetNewTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		_lastVisited[(row, col)] = 0f;

		var dir = Helpers.Directions
			.Where( x => MazeObject.View[row, col, x] == WallState.Open )
			.MaxBy( x => LastVisited( x.GetNeighbor( row, col ) ) + Random.Shared.NextSingle() );

		return dir.GetNeighbor( row, col );
	}
}
