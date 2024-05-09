using System;
using Sandbox.Diagnostics;

namespace Mazing;

partial class BaseMazeGenerator
{
	protected void FixConnectivity( MazeData data, Random random )
	{
		var unvisited = new HashSet<(int Row, int Col)>();

		for ( var j = 0; j < data.Height; ++j )
		{
			for ( var i = 0; i < data.Width; ++i )
			{
				if ( data[j, i] != CellState.Empty )
				{
					unvisited.Add( (j, i) );
				}
			}
		}

		if ( unvisited.Count == 0 )
		{
			return;
		}

		var queue = new Queue<(int Row, int Col)>();
		var walls = new List<(int Row, int Col, Direction Dir)>();

		var directions = new[] { Direction.West, Direction.North, Direction.East, Direction.South };

		var first = unvisited.First();

		unvisited.Remove( first );
		queue.Enqueue( first );

		while ( unvisited.Count > 0 )
		{
			if ( queue.Count == 0 )
			{
				while ( walls.Count > 0 )
				{
					var index = random.Next( walls.Count );
					var wall = walls[index];
					var neighbor = wall.Dir.GetNeighbor( wall.Row, wall.Col );

					walls.RemoveAt( index );

					if ( unvisited.Remove( neighbor ) )
					{
						data[wall.Row, wall.Col, wall.Dir] = WallState.Open;
						queue.Enqueue( neighbor );

						break;
					}
				}

				Assert.True( queue.Count > 0 );
				continue;
			}

			var next = queue.Dequeue();

			foreach ( var direction in directions )
			{
				var neighbor = direction.GetNeighbor( next.Row, next.Col );

				if ( data[next.Row, next.Col, direction] == WallState.Open )
				{
					if ( unvisited.Remove( neighbor ) )
					{
						queue.Enqueue( neighbor );
					}

					continue;
				}

				if ( unvisited.Contains( neighbor ) )
				{
					walls.Add( (next.Row, next.Col, direction) );
				}
			}
		}

		// Knock through a few more walls

		var extraWallCount = Math.Max( 2, walls.Count / 16 );

		while ( walls.Count > 0 && extraWallCount > 0 )
		{
			var index = random.Next( walls.Count );
			var wall = walls[index];

			walls.RemoveAt( index );

			if ( data[wall.Row, wall.Col, wall.Dir] == WallState.Open )
			{
				continue;
			}

			if ( random.NextSingle() < 0.125f ) //  !data.TryFindPath( (wall.Row, wall.Col), Neighbor( wall.Row, wall.Col, wall.Dir ), 8, out _ ) )
			{
				--extraWallCount;
				data[wall.Row, wall.Col, wall.Dir] = WallState.Open;
			}
		}
	}
}
