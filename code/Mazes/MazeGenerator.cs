using System;
using Sandbox.Diagnostics;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size );

public class MazeGenerator
{
	public IMazeDataView Generate( MazeGeneratorParameters parameters )
	{
		var resources = ResourceLibrary.GetAll<MazeChunkResource>()
			.Where( x => !x.IsSpecial )
			.OrderBy( x => x.ResourceId )
			.Where( x => (x.Data.Width & 3) == 0 && (x.Data.Height & 3) == 0 )
			.Select( x => (Size: (x.Data.Width / 4) * (x.Data.Height / 4), x.Data) )
			.ToList();

		var random = new Random( parameters.Seed );
		var blocks = new Dictionary<(int Row, int Col), IMazeDataView>();
		var open = new HashSet<(int Row, int Col)> { (0, 0) };

		var possible = new List<(int Row, int Col, bool Transpose, float Score)>();
		var tested = new HashSet<(int Row, int Col, bool Transpose)>();

		int BiasedRandom( int max, int bias = 1 )
		{
			var value = max - 1;

			for ( var i = 0; i <= bias; ++i )
			{
				value = Math.Min( value, random.Next( max ) );
			}

			return value;
		}

		int? GetScore( int row, int col, int width, int height )
		{
			for ( var j = 0; j < height; ++j )
			{
				for ( var i = 0; i < width; ++i )
				{
					if ( blocks.ContainsKey( (row + j, col + i) ) )
					{
						return null;
					}
				}
			}

			var score = Math.Abs( row ) + Math.Abs( col );

			for ( var i = 0; i < width; ++i )
			{
				if ( blocks.ContainsKey( (row - 1, col + i) ) )
				{
					score -= 1;
				}

				if ( blocks.ContainsKey( (row + height, col + i) ) )
				{
					score -= 1;
				}
			}

			for ( var j = 0; j < height; ++j )
			{
				if ( blocks.ContainsKey( (row + j, col - 1) ) )
				{
					score -= 1;
				}

				if ( blocks.ContainsKey( (row + j, col + width) ) )
				{
					score -= 1;
				}
			}

			return score;
		}

		void TryAddOpen( int row, int col )
		{
			if ( !blocks.ContainsKey( (row, col) ) )
			{
				open.Add( (row, col) );
			}
		}

		while ( blocks.Count < parameters.Size )
		{
			var next = resources[random.Next( resources.Count )];

			if ( next.Size + blocks.Count > parameters.Size )
			{
				resources.RemoveAll( x => x.Size + blocks.Count > parameters.Size );
				continue;
			}

			possible.Clear();
			tested.Clear();

			var width = next.Data.Width / 4;
			var height = next.Data.Height / 4;

			foreach ( var (row, col) in open )
			{
				for ( var rowOffset = 0; rowOffset < height; ++rowOffset )
				{
					for ( var colOffset = 0; colOffset < width; ++colOffset )
					{
						if ( tested.Add( (row - rowOffset, col - colOffset, false) ) )
						{
							if ( GetScore( row - rowOffset, col - colOffset, width, height ) is {} score )
							{
								possible.Add( (row - rowOffset, col - colOffset, false, score + random.NextSingle()) );

								if ( width == height )
								{
									possible.Add( (row - rowOffset, col - colOffset, true, score + random.NextSingle()) );
								}
							}
						}
						
						if ( width == height )
						{
							continue;
						}

						if ( tested.Add( (row - colOffset, col - rowOffset, true) ) )
						{
							if ( GetScore( row - colOffset, col - rowOffset, height, width) is {} score )
							{
								possible.Add( (row - colOffset, col - rowOffset, true, score + random.NextSingle()) );
							}
						}
					}
				}
			}

			possible.Sort( ( a, b ) => a.Score.CompareTo( b.Score ) );

			var placement = possible[BiasedRandom( possible.Count, 2 )];
			var view = (IMazeDataView)next.Data;

			if ( placement.Transpose )
			{
				view = view.Transpose();
				(width, height) = (height, width);
			}

			if ( random.NextSingle() < 0.5f )
			{
				view = view.FlipVertical();
			}

			if ( random.NextSingle() < 0.5f )
			{
				view = view.FlipHorizontal();
			}

			for ( var j = 0; j < height; ++j )
			{
				for ( var i = 0; i < width; ++i )
				{
					open.Remove( (placement.Row + j, placement.Col + i) );
					blocks[(placement.Row + j, placement.Col + i)] = view.Region( i * 4, j * 4, 4, 4 );
				}
			}

			for ( var i = 0; i < width; ++i )
			{
				TryAddOpen( placement.Row - 1, placement.Col + i );
				TryAddOpen( placement.Row + height, placement.Col + i );
			}

			for ( var j = 0; j < height; ++j )
			{
				TryAddOpen( placement.Row + j, placement.Col - 1 );
				TryAddOpen( placement.Row + j, placement.Col + width );
			}
		}

		var minRow = int.MaxValue;
		var minCol = int.MaxValue;
		var maxRow = int.MinValue;
		var maxCol = int.MinValue;

		foreach ( var ((row, col), _) in blocks )
		{
			minRow = Math.Min( minRow, row );
			minCol = Math.Min( minCol, col );
			maxRow = Math.Max( maxRow, row + 1 );
			maxCol = Math.Max( maxCol, col + 1 );
		}

		Assert.True( minRow < maxRow );
		Assert.True( minCol < maxCol );

		var finalData = new MazeData( (maxCol - minCol) * 4, (maxRow - minRow) * 4, null, null, null );

		finalData.Clear( CellState.Empty );

		foreach ( var ((row, col), view) in blocks )
		{
			finalData.CopyFrom( view, (row - minRow) * 4, (col - minCol) * 4 );
		}

		return finalData;
	}
}

