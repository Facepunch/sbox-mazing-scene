using System;

namespace Mazing;

public enum WallState
{
	Open,
	Closed
}

public enum CellState
{
	Default,

	Empty,

	[Icon( "key" )]
	Key,

	[Icon( "logout" )]
	Exit,

	[Icon( "accessibility_new" )]
	Player,

	[Icon( "diamond" )]
	Treasure,

	[Icon( "sentiment_dissatisfied" )]
	Enemy,

	Count
}

public enum Direction
{
	West,
	North,
	East,
	South,
}

public static class DirectionExtensions
{
	public static (int Row, int Col) GetNeighbor( this Direction dir, int row, int col, int range = 1 )
	{
		return dir switch
		{
			Direction.North => (row - range, col),
			Direction.South => (row + range, col),
			Direction.West => (row, col - range),
			Direction.East => (row, col + range),
			_ => throw new ArgumentException()
		};
	}

	public static Direction Clockwise( this Direction dir )
	{
		return dir switch
		{
			Direction.North => Direction.East,
			Direction.East => Direction.South,
			Direction.South => Direction.West,
			Direction.West => Direction.North,
			_ => throw new ArgumentException()
		};
	}

	public static Direction CounterClockwise( this Direction dir )
	{
		return dir switch
		{
			Direction.North => Direction.West,
			Direction.East => Direction.North,
			Direction.South => Direction.East,
			Direction.West => Direction.South,
			_ => throw new ArgumentException()
		};
	}

	public static Direction GetDirection( this Vector2 vec )
	{
		if ( MathF.Abs( vec.x ) > MathF.Abs( vec.y ) )
		{
			return vec.x < 0f ? Direction.North : Direction.South;
		}

		return vec.y < 0f ? Direction.West : Direction.East;
	}

	public static Vector2 GetNormal( this Direction dir )
	{
		return dir switch
		{
			Direction.North => new( -1, 0f ),
			Direction.South => new( 1, 0f ),
			Direction.West => new( 0f, -1f ),
			Direction.East => new( 0f, 1f ),
			_ => throw new ArgumentException()
		};
	}
}
