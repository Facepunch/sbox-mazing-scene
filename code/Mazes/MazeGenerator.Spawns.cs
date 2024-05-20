﻿
using System;
using System.Collections.Immutable;

namespace Mazing;

partial class BaseMazeGenerator
{
	protected IReadOnlyDictionary<CellState, IReadOnlyList<(int Row, int Col)>> PlaceSpawns( int seed, MazeData data, int enemyCount, int treasureCount )
	{
		var random = new Random( seed );
		var validCells = new List<(int Row, int Col)>();
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

		// Check for hand-placed spawns in the maze

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
						results[state].Add( index );
						continue;
					}

					data[index.Row, index.Col] = CellState.Default;
				}
			}

			validCells.Add( index );
		}

		// Fill up quotas randomly

		foreach ( var (state, count) in remainingCounts )
		{
			if ( count <= 0 )
			{
				continue;
			}

			if ( state == CellState.Exit )
			{
				var cell = PlaceExit( random, data, validCells, treasureCount );
				data[cell.Row, cell.Col] = state;
				results[state].Add( cell );

				continue;
			}

			for ( var i = 0; i < count && validCells.Count > 0; ++i )
			{
				var cell = validCells[^1];
				validCells.RemoveAt( validCells.Count - 1 );

				data[cell.Row, cell.Col] = state;
				results[state].Add( cell );
			}
		}

		return results.ToImmutableDictionary( x => x.Key, x => (IReadOnlyList<(int, int)>) x.Value );
	}

	private (int Row, int Col) PlaceExit( Random random, MazeData data, List<(int Row, int Col)> validCells, int treasureCount )
	{
		var bestIndex = -1;
		var bestScore = int.MaxValue;

		var idealHeldCount = random.Next( 1, Math.Clamp( treasureCount / 2, 1, 4 ) );

		for ( var i = 0; i < validCells.Count; ++i )
		{
			var cell = validCells[i];
			var score = 0;
			var possible = false;

			foreach ( var direction in Helpers.Directions )
			{
				var neighbor = direction.GetNeighbor( cell.Row, cell.Col );

				if ( data[neighbor.Row, neighbor.Col] == CellState.Empty )
				{
					score += 4;
				}

				for ( var heldTreasure = -1; heldTreasure <= treasureCount; ++heldTreasure )
				{
					var from = direction.GetNeighbor( cell.Row, cell.Col, heldTreasure + 2 );

					if ( data[from.Row, from.Col] == CellState.Empty )
					{
						break;
					}

					if ( heldTreasure < 0 )
					{
						continue;
					}

					if ( data[from.Row, from.Col, direction.Opposite()] != WallState.Closed )
					{
						continue;
					}

					possible = true;
					score += Math.Abs( idealHeldCount - heldTreasure ) * (Math.Max( idealHeldCount - heldTreasure, 0 ) + 1);
				}
			}

			if ( !possible )
			{
				continue;
			}

			if ( score < bestScore )
			{
				bestIndex = i;
				bestScore = score;
			}
		}

		var best = validCells[bestIndex];
		validCells.RemoveAt( bestIndex );

		return best;
	}
}
