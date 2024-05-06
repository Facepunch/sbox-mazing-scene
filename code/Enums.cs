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
