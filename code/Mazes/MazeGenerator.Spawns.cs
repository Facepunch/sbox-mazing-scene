
using System;

namespace Mazing;

partial class MazeGenerator
{
	private void PlaceSpawns( MazeData data, int treasureCount, int enemyCount, Random random )
	{
		var validCells = new Queue<(int Row, int Col)>();
		var remainingCounts = new Dictionary<CellState, int>
		{
			{ CellState.Key, 1 },
			{ CellState.Exit, 1 },
			{ CellState.Player, 8 },
			{ CellState.Treasure, treasureCount },
			{ CellState.Enemy, enemyCount }
		};

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
						remainingCounts[state] = remaining - 1;
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
			}
		}
	}
}
