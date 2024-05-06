using System;

namespace Mazing;

public static class AStar
{
	public static bool TryFindPath( this IMazeDataView view,
		(int Row, int Col) start, (int Row, int Col) end, int maxDist, out IReadOnlyList<(int Row, int Col)> outPath )
	{
		static int H( (int Row, int Col) pos, (int Row, int Col) end )
		{
			return Math.Abs( pos.Row - end.Row ) + Math.Abs( pos.Col - end.Col );
		}

		var open = new HashSet<(int Row, int Col)> { start };
		var from = new Dictionary<(int Row, int Col), (int Row, int Col)>();
		var gScore = new Dictionary<(int Row, int Col), int> { { start, 0 } };
		var fScore = new Dictionary<(int Row, int Col), int> { { start, H( start, end ) } };

		throw new NotImplementedException();

		while ( open.Count > 0 )
		{
			throw new NotImplementedException();
		}
	}
}

