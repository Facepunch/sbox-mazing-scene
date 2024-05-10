using System;

namespace Mazing;

public static class AStar
{
	public static IReadOnlyList<(int Row, int Col)>? FindPath( this IMazeDataView view,
		(int Row, int Col) start, (int Row, int Col) end, int maxDist )
	{
		static int H( (int Row, int Col) pos, (int Row, int Col) end )
		{
			return Math.Abs( pos.Row - end.Row ) + Math.Abs( pos.Col - end.Col );
		}

		var open = new PriorityQueue<(int Row, int Col), int>();
		var from = new Dictionary<(int Row, int Col), (int Row, int Col)>();
		var gScore = new Dictionary<(int Row, int Col), int> { { start, 0 } };
		var fScore = new Dictionary<(int Row, int Col), int> { { start, H( start, end ) } };

		open.Enqueue( start, H( start, end ) );

		IReadOnlyList<(int Row, int Col)> ReconstructPath()
		{
			var path = new List<(int Row, int Col)>();
			var head = end;

			path.Add( end );

			while ( from.TryGetValue( head, out var prev ) )
			{
				path.Add( prev );
				head = prev;
			}

			path.Reverse();

			return path;
		}

		while ( open.TryDequeue( out var current, out _ ) )
		{
			if ( current == end )
			{
				return ReconstructPath();
			}

			var gTentative = gScore[current] + 1;

			if ( gTentative > maxDist )
			{
				continue;
			}

			foreach ( var direction in Helpers.Directions )
			{
				if ( view[current.Row, current.Col, direction] != WallState.Open )
				{
					continue;
				}

				var neighbor = direction.GetNeighbor( current.Row, current.Col );

				if ( gScore.TryGetValue( neighbor, out var gNeighbor ) && gTentative >= gNeighbor )
				{
					continue;
				}

				var fNeighbor = gTentative + H( neighbor, end );

				from[neighbor] = current;
				gScore[neighbor] = gTentative;
				fScore[neighbor] = fNeighbor;

				if ( fNeighbor <= maxDist )
				{
					open.Enqueue( neighbor, fNeighbor );
				}
			}
		}

		return null;
	}
}

