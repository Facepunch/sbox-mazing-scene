using System;

namespace Mazing;

public class Wanderer : Navigator
{
	protected override Direction? GetNewTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		var dir = Helpers.Directions
			.Where( x => MazeObject.View[row, col, x] == WallState.Open )
			.MaxBy( x => LastVisited( x.GetNeighbor( row, col ) ) + Random.Shared.NextSingle() );

		return dir;
	}
}
