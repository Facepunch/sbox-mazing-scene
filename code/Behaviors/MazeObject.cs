using System;

namespace Mazing;

public sealed class MazeObject : Component
{
	private TestMaze? _maze;

	public TestMaze Maze => _maze is null or { IsValid: false }
		? _maze = Scene.Components.Get<TestMaze>( FindMode.Enabled | FindMode.InChildren )
			?? throw new Exception( "Not in a maze!" )
		: _maze;

	public IMazeDataView View => Maze.View ?? throw new Exception( "No maze view!" );

	public Vector2 MazeLocalPos => Maze?.WorldToMazePos( Transform.Position ) ?? default;

	public Vector2 CellLocalPos
	{
		get
		{
			var mazeLocalPos = MazeLocalPos;
			return mazeLocalPos - new Vector2( MathF.Floor( mazeLocalPos.x ), MathF.Floor( mazeLocalPos.y ) );
		}
	}

	public (int Row, int Col) CellIndex
	{
		get
		{
			if ( Maze is null )
			{
				return (0, 0);
			}

			var mazePos = Maze.WorldToMazePos( Transform.Position );
			return ((int)MathF.Floor( mazePos.x ), (int)MathF.Floor( mazePos.y ));
		}
	}
}

