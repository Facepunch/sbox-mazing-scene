﻿using System;

namespace Mazing;

public sealed class MazeObject : Component
{
	private Maze? _maze;

	public Maze Maze => _maze is null or { IsValid: false }
		? _maze = Scene.Components.Get<Maze>( FindMode.Enabled | FindMode.InChildren )
			?? throw new Exception( "Not in a maze!" )
		: _maze;

	public IMazeDataView View => Maze.View ?? throw new Exception( "No maze view!" );

	public Vector2 MazeLocalPos => Maze?.WorldToMazePos( WorldPosition ) ?? default;

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
			var mazePos = Maze.WorldToMazePos( WorldPosition );
			return ((int)MathF.Floor( mazePos.x ), (int)MathF.Floor( mazePos.y ));
		}
	}

	public IEnumerable<MazeObject> GetObjectsInSameCell()
	{
		var (row, col) = CellIndex;
		return Maze.GetObjectsInCell( row, col )
			.Where( x => x != this );
	}
}

public static class MazeObjectExtensions
{
	[ActionGraphNode("mazing.touchingHoldable")]
	public static bool IsTouchingHoldable( this MazeObject obj )
	{
		return obj.GetObjectsInSameCell().Any( x => x != obj && x.Components.Get<Holdable>() is not null );
	}
}

