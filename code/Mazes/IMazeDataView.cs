using System;

namespace Mazing;

public interface IMazeDataView
{
	int Width { get; }
	int Height { get; }

	WallState this[int row, int col, Direction dir] { get; }
	CellState this[int row, int col] { get; }

	public IEnumerable<(int Row, int Col, CellState State)> Cells => Enumerable.Range( 0, Height )
		.SelectMany( j => Enumerable.Range( 0, Width )
			.Select( i => (Row: j, Col: i, State: this[j, i]) ) )
		.Where( x => x.State != CellState.Empty );

	public IEnumerable<(int Row, int Col)> GetCells( CellState state )
	{
		for ( var j = 0; j < Height; ++j )
		{
			for ( var i = 0; i < Width; ++i )
			{
				if ( this[j, i] == state )
				{
					yield return (j, i);
				}
			}
		}
	}
}

public static class MazeDataViewExtensions
{
	private record TransposeMazeDataView( IMazeDataView Inner ) : IMazeDataView
	{
		public int Width => Inner.Height;

		public int Height => Inner.Width;

		public WallState this[ int row, int col, Direction dir ] => Inner[col, row, dir.Transpose()];

		public CellState this[ int row, int col ] => Inner[col, row];
	}

	private record FlipHorizontalMazeDataView( IMazeDataView Inner ) : IMazeDataView
	{
		public int Width => Inner.Width;

		public int Height => Inner.Height;

		public WallState this[ int row, int col, Direction dir ] => Inner[row, Width - col - 1, dir switch
		{
			Direction.East or Direction.West => dir.Opposite(),
			_ => dir
		}];

		public CellState this[int row, int col] => Inner[row, Width - col - 1];
	}

	private record FlipVerticalMazeDataView( IMazeDataView Inner ) : IMazeDataView
	{
		public int Width => Inner.Width;

		public int Height => Inner.Height;

		public WallState this[ int row, int col, Direction dir ] => Inner[Height - row - 1, col, dir switch
		{
			Direction.North or Direction.South => dir.Opposite(),
			_ => dir
		}];

		public CellState this[int row, int col] => Inner[Height - row - 1, col];
	}

	private record RegionMazeDataView( IMazeDataView Inner, int ColOffset, int RowOffset, int Width, int Height ) : IMazeDataView
	{
		public WallState this[ int row, int col, Direction dir ] => Inner[row + RowOffset, col + ColOffset, dir];

		public CellState this[ int row, int col ] => Inner[row + RowOffset, col + ColOffset];
	}

	public static IMazeDataView Transpose( this IMazeDataView view )
	{
		if ( view is TransposeMazeDataView transpose )
		{
			return transpose.Inner;
		}

		return new TransposeMazeDataView( view );
	}

	public static IMazeDataView FlipHorizontal( this IMazeDataView view )
	{
		if ( view is FlipHorizontalMazeDataView flip )
		{
			return flip.Inner;
		}

		return new FlipHorizontalMazeDataView( view );
	}

	public static IMazeDataView FlipVertical( this IMazeDataView view )
	{
		if ( view is FlipVerticalMazeDataView flip )
		{
			return flip.Inner;
		}

		return new FlipVerticalMazeDataView( view );
	}

	public static IMazeDataView Region( this IMazeDataView view, int colOffset, int rowOffset, int width, int height )
	{
		return new RegionMazeDataView( view, colOffset, rowOffset, width, height );
	}

	public static Direction Transpose( this Direction dir )
	{
		return dir switch
		{
			Direction.North => Direction.West,
			Direction.West => Direction.North,
			Direction.South => Direction.East,
			Direction.East => Direction.South,
			_ => throw new ArgumentException()
		};
	}

	public static Direction Opposite( this Direction dir )
	{
		return dir switch
		{
			Direction.North => Direction.South,
			Direction.South => Direction.North,
			Direction.West => Direction.East,
			Direction.East => Direction.West,
			_ => throw new ArgumentException()
		};
	}
}
