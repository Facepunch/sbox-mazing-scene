using System;

namespace Mazing;

public sealed partial class Maze : Component
{
	[Property, Group("Parameters")]
	public int Seed { get; set; } = 0x12345678;

	[Property, Group( "Parameters" )]
	public int Size { get; set; } = 4;

	[Property, Group( "Parameters" )]
	public int TreasureCount { get; set; } = 8;

	[Property, Group( "Parameters" )]
	public int EnemyCount { get; set; } = 4;

	[Button( "Run", "casino" ), Group( "Parameters" )]
	public void Randomize()
	{
		Seed = Random.Shared.Next();

		Generate();
	}

	public IMazeDataView? View { get; private set; }

	public Vector2 WorldToMazePos( Vector3 pos )
	{
		return Transform.World.PointToLocal( pos ) / 48f;
	}

	public Vector3 MazeToWorldPos( Vector2 pos )
	{
		return Transform.World.PointToWorld( pos * 48f );
	}

	public Vector3 MazeToWorldPos( int row, int col )
	{
		return Transform.World.PointToWorld( new Vector2( row + 0.5f, col + 0.5f ) * 48f );
	}

	public Rect GetCellWorldRect( int row, int col )
	{
		var min = Transform.World.PointToWorld( new Vector3( row, col ) * 48f );
		var max = Transform.World.PointToWorld( new Vector3( row + 1, col + 1 ) * 48f );

		return new Rect( min, max - min );
	}

	[Broadcast( NetPermission.HostOnly )]
	public void NextLevel( int size, int seed )
	{
		Size = size;
		Seed = seed;

		Generate();
	}

	[Button( "Run", "casino" ), Group( "Parameters" )]
	private void Generate()
	{
		using var _ = Scene.Push();

		DestroySpawnedObjects();

		foreach ( var child in GameObject.Children )
		{
			child.Destroy();
		}

		IMazeGenerator generator = Size <= 0 ? new LobbyMazeGenerator() : new MazeGenerator();

		var result = generator.Generate( new MazeGeneratorParameters( Seed, Size, TreasureCount, EnemyCount ) );

		View = result.View;

		UpdateGeometry( result );

		SpawnObjects();
	}

	protected override void OnStart()
	{
		base.OnStart();

		Generate();
	}
}
