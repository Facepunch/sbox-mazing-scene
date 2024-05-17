
using System;
using System.Collections.Immutable;

namespace Mazing;

partial class BaseMazeGenerator
{
	protected IReadOnlyDictionary<CellState, IReadOnlyList<(int Row, int Col)>> PlaceSpawns( int seed, MazeData data, int enemyCount, int treasureCount )
	{
		var random = new Random( seed );
		var validCells = new Queue<(int Row, int Col)>();
		var remainingCounts = new Dictionary<CellState, int>
		{
			{ CellState.Key, 1 },
			{ CellState.Exit, 1 },
			{ CellState.Player, 8 },
			{ CellState.Enemy, enemyCount },
			{ CellState.Treasure, treasureCount }
		};

		var results = remainingCounts.Keys
			.ToDictionary( x => x, x => new List<(int Row, int Col)>() );

		var indices = Enumerable.Range( 0, data.Height )
			.SelectMany( j => Enumerable.Range( 0, data.Width )
				.Select( i => (Row: j, Col: i) ) )
			.ToArray();

		indices.Shuffle( random );

		foreach ( var index in indices )
		{
			if ( data[index.Row, index.Col] is { } state and not CellState.Default )
			{
				if ( state == CellState.Empty )
				{
					continue;
				}

				if ( remainingCounts.TryGetValue( state, out var remaining ) )
				{
					if ( remaining > 0 )
					{
						Log.Info( $"{state}, {remaining}, {index}" );

						remainingCounts[state] = remaining - 1;
						results[state].Add( index );
						continue;
					}

					data[index.Row, index.Col] = CellState.Default;
				}
			}

			validCells.Enqueue( index );
		}

		foreach ( var (state, count) in remainingCounts )
		{
			for ( var i = 0; i < count && validCells.Count > 0; ++i )
			{
				var cell = validCells.Dequeue();
				data[cell.Row, cell.Col] = state;
				results[state].Add( cell );
			}
		}

		return results.ToImmutableDictionary( x => x.Key, x => (IReadOnlyList<(int, int)>) x.Value );
	}
}
