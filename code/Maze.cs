using System;
using System.Runtime.InteropServices;

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

	[Property, Group( "Parameters" )]
	public int Level { get; set; } = 1;

	private readonly List<MazeObject> _allObjects = new();
	private readonly Dictionary<(int Row, int Col), (int Offset, int Count)> _objectsInCells = new();

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
	public void NextLevel( int level, int size, int enemyCount, int treasureCount, int seed )
	{
		Level = level;
		Size = size;
		EnemyCount = enemyCount;
		TreasureCount = treasureCount;
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

		IMazeGenerator generator = Level == 0 ? new LobbyMazeGenerator() : new MazeGenerator();

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

	protected override void OnUpdate()
	{
		_allObjects.Clear();
		_objectsInCells.Clear();

		foreach ( var mazeObject in Scene.Components.GetAll<MazeObject>( FindMode.Enabled | FindMode.InChildren ) )
		{
			if ( mazeObject.Components.Get<Holdable>() is { Holder: not null } )
			{
				continue;
			}

			_allObjects.Add( mazeObject );
		}

		_allObjects.Sort( ( a, b ) => a.CellIndex.CompareTo( b.CellIndex ) );

		var offset = 0;
		var count = 0;

		(int Row, int Col) prevIndex = (int.MinValue, int.MinValue);

		for ( var i = 0; i < _allObjects.Count; i++ )
		{
			var mazeObject = _allObjects[i];
			var index = mazeObject.CellIndex;

			if ( index == prevIndex )
			{
				++count;
			}
			else
			{
				if ( count > 0 )
				{
					_objectsInCells[prevIndex] = (offset, count);
				}

				prevIndex = index;
				offset = i;
				count = 1;
			}
		}

		if ( count > 0 )
		{
			_objectsInCells[prevIndex] = (offset, count);
		}
	}

	public IEnumerable<MazeObject> GetObjectsInCell( int row, int col )
	{
		if ( !_objectsInCells.TryGetValue( (row, col), out var slice ) )
		{
			return Array.Empty<MazeObject>();
		}

		return _allObjects
			.Skip( slice.Offset )
			.Take( slice.Count )
			.Where( x => x.Components.Get<Holdable>() is not { Holder: not null } )
			.Where( x => x.Components.Get<Throwable>() is not { Enabled: true } )
			.OrderBy( x => x.Components.Get<Throwable>()?.IndexOnFloor ?? -1 );
	}
}
