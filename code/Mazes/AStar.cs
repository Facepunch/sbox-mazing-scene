using System;

namespace Mazing;

public static class AStar
{
	public delegate int CostFunc( int row, int col );

	private static int H( (int Row, int Col) pos, (int Row, int Col) end )
	{
		return Math.Abs( pos.Row - end.Row ) + Math.Abs( pos.Col - end.Col );
	}

	private static int H( (int Row, int Col) pos, IReadOnlyList<(int Row, int Col)> ends )
	{
		var minDist = int.MaxValue;

		foreach ( var end in ends )
		{
			minDist = Math.Min( minDist, Math.Abs( pos.Row - end.Row ) + Math.Abs( pos.Col - end.Col ) );
		}

		return minDist;
	}

	private static IReadOnlyList<(int Row, int Col)> ReconstructPath( Dictionary<(int Row, int Col), (int Row, int Col)> from, (int Row, int Col) end )
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

	public static IReadOnlyList<(int Row, int Col)>? FindPath( this IMazeDataView view,
		(int Row, int Col) start, (int Row, int Col) end, int maxDist = int.MaxValue,
		CostFunc? getCost = null )
	{
		var open = new PriorityQueue<(int Row, int Col), int>();
		var from = new Dictionary<(int Row, int Col), (int Row, int Col)>();
		var gScore = new Dictionary<(int Row, int Col), (int Dist, int Cost)> { { start, (0, 0) } };
		var fScore = new Dictionary<(int Row, int Col), int> { { start, H( start, end ) } };
		var costs = new Dictionary<(int Row, int Col), int>();

		open.Enqueue( start, H( start, end ) );

		while ( open.TryDequeue( out var current, out _ ) )
		{
			if ( current == end )
			{
				return ReconstructPath( from, end );
			}

			var gCurrent = gScore[current];

			if ( gCurrent.Dist + 1 > maxDist )
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
				var cost = getCost is null ? 0 : costs.TryGetValue( neighbor, out var c ) ? c : costs[neighbor] = getCost( neighbor.Row, neighbor.Col );

				var gTentative = gCurrent.Cost + cost;

				if ( gScore.TryGetValue( neighbor, out var gNeighbor ) && gTentative >= gNeighbor.Cost )
				{
					continue;
				}

				var fNeighbor = gTentative + H( neighbor, end );

				from[neighbor] = current;
				gScore[neighbor] = (gCurrent.Dist + 1, gTentative);
				fScore[neighbor] = fNeighbor;

				if ( fNeighbor <= maxDist )
				{
					open.Enqueue( neighbor, fNeighbor );
				}
			}
		}

		return null;
	}

	public static IReadOnlyList<(int Row, int Col)>? FindPathToAny( this IMazeDataView view,
		(int Row, int Col) start, IReadOnlyList<(int Row, int Col)> ends, int maxDist = int.MaxValue,
		CostFunc? getCost = null )
	{
		var endSet = new HashSet<(int Row, int Col)>( ends );

		var open = new PriorityQueue<(int Row, int Col), int>();
		var from = new Dictionary<(int Row, int Col), (int Row, int Col)>();
		var gScore = new Dictionary<(int Row, int Col), int> { { start, 0 } };
		var fScore = new Dictionary<(int Row, int Col), int> { { start, H( start, ends ) } };
		var costs = new Dictionary<(int Row, int Col), int>();

		open.Enqueue( start, H( start, ends ) );

		while ( open.TryDequeue( out var current, out _ ) )
		{
			if ( endSet.Contains( current ) )
			{
				return ReconstructPath( from, current );
			}

			var gCurrent = gScore[current];

			if ( gCurrent + 1 > maxDist )
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
				var cost = getCost is null ? 0 : costs.TryGetValue( neighbor, out var c ) ? c : costs[neighbor] = getCost( neighbor.Row, neighbor.Col );

				var gTentative = gCurrent + cost;

				if ( gScore.TryGetValue( neighbor, out var gNeighbor ) && gTentative >= gNeighbor )
				{
					continue;
				}

				var fNeighbor = gTentative + H( neighbor, ends );

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

