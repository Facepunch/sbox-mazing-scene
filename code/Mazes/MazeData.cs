using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Mazing;

[JsonConverter( typeof( MazeDataConverter ) )]
public class MazeData : IMazeDataView
{
	private WallState[] _vertWalls = null!;
	private WallState[] _horzWalls = null!;
	private CellState[] _cells = null!;

	private int _width = 8;
	private int _height = 8;

	public int Width
	{
		get => _width;
		set
		{
			_width = value;

			Validate();
			Clear();
			AddOuterWalls();
		}
	}

	public int Height
	{
		get => _height;
		set
		{
			_height = value;

			Validate();
			Clear();
			AddOuterWalls();
		}
	}

	public int ValueHash
	{
		get
		{
			var hash = new HashCode();

			hash.Add( Width );
			hash.Add( Height );

			foreach ( var wallState in _vertWalls )
			{
				hash.Add( wallState );
			}

			foreach ( var wallState in _horzWalls )
			{
				hash.Add( wallState );
			}

			foreach ( var cellState in _cells )
			{
				hash.Add( cellState );
			}

			return hash.ToHashCode();
		}
	}

	public IReadOnlyList<WallState> VertWalls => _vertWalls;
	public IReadOnlyList<WallState> HorzWalls => _horzWalls;
	public IReadOnlyList<CellState> Cells => _cells;

	public MazeData()
	{
		Validate();
		AddOuterWalls();
	}

	internal MazeData( int width, int height, WallState[]? vertWalls, WallState[]? horzWalls, CellState[]? cells )
	{
		_width = width;
		_height = height;

		_vertWalls = vertWalls;
		_horzWalls = horzWalls;
		_cells = cells;

		Validate();
	}

	private void Validate()
	{
		Array.Resize( ref _vertWalls, (Width + 1) * Height );
		Array.Resize( ref _horzWalls, (Height + 1) * Width );
		Array.Resize( ref _cells, Width * Height );
	}

	public void Clear()
	{
		Array.Clear( _vertWalls );
		Array.Clear( _horzWalls );
		Array.Clear( _cells );
	}

	public void AddOuterWalls()
	{
		for ( var i = 0; i < Width; ++i )
		{
			this[0, i, Direction.North] = WallState.Closed;
			this[Height - 1, i, Direction.South] = WallState.Closed;
		}

		for ( var i = 0; i < Height; ++i )
		{
			this[i, 0, Direction.West] = WallState.Closed;
			this[i, Width - 1, Direction.East] = WallState.Closed;
		}
	}

	public void CopyFrom( IMazeDataView view, int dstRowOffset = 0, int dstColOffset = 0 )
	{
		var width = Math.Min( view.Width, Width - dstColOffset );
		var height = Math.Min( view.Height, Height - dstRowOffset );

		for ( var j = 0; j < height; ++j )
		{
			for ( var i = 0; i < width; ++i )
			{
				this[i + dstRowOffset, j + dstColOffset] = view[i, j];
			}
		}

		for ( var j = 0; j <= height; ++j )
		{
			for ( var i = 0; i < width; ++i )
			{
				this[i + dstRowOffset, j + dstColOffset, Direction.North] = view[i, j, Direction.North];
			}
		}

		for ( var j = 0; j < height; ++j )
		{
			for ( var i = 0; i <= width; ++i )
			{
				this[i + dstRowOffset, j + dstColOffset, Direction.West] = view[i, j, Direction.West];
			}
		}
	}

	public WallState this[int row, int col, Direction dir]
	{
		get => dir switch
		{
			Direction.East => this[row, col + 1, Direction.West],
			Direction.South => this[row + 1, col, Direction.North],
			Direction.West => row < 0 || row >= Height || col < 0 || col > Width
				? WallState.Open
				: _vertWalls[col + row * (Width + 1)],
			Direction.North => row < 0 || row > Height || col < 0 || col >= Width
				? WallState.Open
				: _horzWalls[col + row * Width],
			_ => WallState.Open
		};

		set
		{
			switch ( dir )
			{
				case Direction.East:
					this[row, col + 1, Direction.West] = value;
					return;

				case Direction.South:
					this[row + 1, col, Direction.North] = value;
					return;

				case Direction.West:
					if ( row < 0 || row >= Height || col < 0 || col > Width ) return;
					_vertWalls[col + row * (Width + 1)] = value;
					return;

				case Direction.North:
					if ( row < 0 || row > Height || col < 0 || col >= Width ) return;
					_horzWalls[col + row * Width] = value;
					return;
			}
		}
	}

	public CellState this[int row, int col]
	{
		get => row < 0 || row >= Height || col < 0 || col >= Width ? CellState.Empty : _cells[col + row * Width];
		set
		{
			if ( row < 0 || row >= Height || col < 0 || col >= Width ) return;
			_cells[col + row * Width] = value;
		}
	}
}

internal class MazeDataConverter : JsonConverter<MazeData>
{
	internal record Model( int Width, int Height, string VertWalls, string HorzWalls, string Cells );

	public override MazeData? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		var model = JsonSerializer.Deserialize<Model>( ref reader, options );

		if ( model is null )
		{
			return null;
		}

		return new MazeData( model.Width, model.Height,
			model.VertWalls.Deserialize<WallState>()?.ToArray(),
			model.HorzWalls.Deserialize<WallState>()?.ToArray(),
			model.Cells.Deserialize<CellState>()?.ToArray() );
	}

	public override void Write( Utf8JsonWriter writer, MazeData value, JsonSerializerOptions options )
	{
		JsonSerializer.Serialize( writer, new Model( value.Width, value.Height, value.VertWalls.Serialize(), value.HorzWalls.Serialize(), value.Cells.Serialize() ), options );
	}
}
