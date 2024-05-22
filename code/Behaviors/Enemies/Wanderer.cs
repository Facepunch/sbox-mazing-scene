using System;

namespace Mazing;

public class Wanderer : Navigator
{
	protected override Direction? GetNewTarget()
	{
		var (row, col) = MazeObject.CellIndex;

		var dir = ValidDirections
			.MaxBy( x => LastVisited( x.GetNeighbor( row, col ) ) + Random.Shared.NextSingle() );

		return dir;
	}
}
