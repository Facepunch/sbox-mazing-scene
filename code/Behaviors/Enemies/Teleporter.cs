using System;

namespace Mazing;

public sealed class Teleporter : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Mazer Mazer { get; set; } = null!;

	[Property]
	public int MinPlayerDistance { get; set; } = 1;

	[Property]
	public int MaxPlayerDistance { get; set; } = 8;

	[Property]
	public event Action? Departed;

	[Property]
	public event Action? Arrived;

	[Property]
	public event Action? ChargingUp;

	[Property, Sync]
	public Vector3 DepartPosition { get; set; }

	[Property, Sync]
	public Direction DepartDirection { get; set; }

	private (int Row, int Col) _arriveCell;

	[Property, Sync]
	public Vector3 ArrivePosition { get; set; }

	[Property, Sync]
	public Direction ArriveDirection { get; set; }

	protected override void OnStart()
	{
		DepartPosition = ArrivePosition = Transform.Position;
		DepartDirection = ArriveDirection = Mazer.Direction;
	}

	public void ChargeUp()
	{
		DispatchChargingUp();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchChargingUp()
	{
		ChargingUp?.Invoke();
	}

	public void Depart()
	{
		ChooseDestination();
		DispatchDeparted();
	}

	private static int Distance( (int Row, int Col) a, (int Row, int Col) b )
	{
		return Math.Max( Math.Abs( a.Row - b.Row ), Math.Abs( a.Col - b.Col ) );
	}

	private static bool IsBetween( int value, int min, int max )
	{
		return value >= min && value <= max;
	}

	private void ChooseDestination()
	{
		var maze = MazeObject.Maze;

		var playerCells = Scene.Components
			.GetAll<Player>( FindMode.Enabled | FindMode.InChildren )
			.Where( x => x.Mazer.State is MazerState.Walking or MazerState.Vaulting )
			.Select( x => x.Mazer.MazeObject.CellIndex )
			.ToArray();

		var teleporterCells = Scene.Components
			.GetAll<Teleporter>( FindMode.EverythingInChildren )
			.Select( x => x._arriveCell )
			.ToHashSet();

		var spawns = MazeObject.Maze.RangedUnitSpawns
			.Where( x => playerCells.Length == 0 || IsBetween( playerCells.Min( y => Distance( (x.Row, x.Col), y ) ), MinPlayerDistance, MaxPlayerDistance ) )
			.Where( x => !teleporterCells.Contains( (x.Row, x.Col) ) )
			.ToArray();

		spawns.Shuffle( Random.Shared );

		var spawn = spawns.MaxBy( x => Math.Min( x.Range, 3 ) );

		SetDestination( spawn.Row, spawn.Col, spawn.Direction );
	}

	private void SetDestination( int row, int col, Direction dir )
	{
		_arriveCell = (row, col);

		DepartPosition = Transform.Position;
		DepartDirection = Mazer.Direction;

		ArrivePosition = MazeObject.Maze.MazeToWorldPos( row, col );
		ArriveDirection = dir;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchDeparted()
	{
		Departed?.Invoke();

		GameObject.Enabled = false;
	}

	public void Arrive()
	{
		DispatchArrived();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchArrived()
	{
		Transform.Position = ArrivePosition;
		Transform.Rotation = ArriveDirection.GetRotation();

		Mazer.Direction = ArriveDirection;

		GameObject.Enabled = true;

		Arrived?.Invoke();
	}
}
