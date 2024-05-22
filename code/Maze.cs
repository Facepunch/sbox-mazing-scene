using System;

namespace Mazing;

public sealed partial class Maze : Component
{
	[Property, Group("Parameters")]
	public int Seed { get; set; } = 0x12345678;

	[Property, Group( "Parameters" )]
	public int Level { get; set; } = 1;

	public delegate void MazeParamFunc( int level, out int size, out int treasureCount, out int enemyCount );

	[Property] public MazeParamFunc GetLevelParameters { get; set; } = null!;

	private readonly List<MazeObject> _allObjects = new();
	private readonly Dictionary<(int Row, int Col), (int Offset, int Count)> _objectsInCells = new();
	private readonly Dictionary<(int Row, int Col), int> _pathCosts = new();

	public record struct RangedUnitSpawn( int Row, int Col, Direction Direction, int Range );

	public IReadOnlyList<RangedUnitSpawn> RangedUnitSpawns { get; private set; } = Array.Empty<RangedUnitSpawn>();

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
	public void NextLevel( int level, int seed )
	{
		Level = level;
		Seed = seed;

		Generate();
	}

	private MazeGeneratorParameters GetParameters( int seed, int level )
	{
		if ( level == 0 )
		{
			return new MazeGeneratorParameters( seed, level, 4, 2, 1 );
		}

		GetLevelParameters( level, out var size, out var treasureCount, out var enemyCount );

		return new MazeGeneratorParameters( seed, level, size, treasureCount, enemyCount );
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

		var parameters = GetParameters( Seed, Level );
		var result = generator.Generate( parameters );

		View = result.View;

		RangedUnitSpawns = View.Cells
			.Where( x => x.State is not CellState.Empty and not CellState.Exit )
			.SelectMany( x => Helpers.Directions
				.Select( dir =>
					new RangedUnitSpawn( x.Row, x.Col, dir, result.View.GetMaxRange( x.Row, x.Col, dir ) ) ) )
			.Where( x => x.Range > 0 )
			.Where( x => result.View[x.Row, x.Col, x.Direction.Opposite()] == WallState.Closed )
			.ToArray();

		UpdateGeometry( result );

		SpawnObjects( result );
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
		_pathCosts.Clear();

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
		var cost = 0;

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
					_pathCosts[prevIndex] = cost;
				}

				prevIndex = index;
				offset = i;
				count = 1;
				cost = 0;
			}

			cost += mazeObject.Components.Get<Enemy>() is null ? 0 : 32;
			cost += mazeObject.Components.Get<Exit>() is { IsOpen: true } ? 256 : 0;
		}

		if ( count > 0 )
		{
			_objectsInCells[prevIndex] = (offset, count);
			_pathCosts[prevIndex] = cost;
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
			.Where( x => x.Components.Get<Throwable>() is not { IsAirborne: true } )
			.Where( x => x.Components.Get<Mazer>() is not { State: not MazerState.Walking } )
			.OrderByDescending( x => x.Components.Get<Throwable>( true )?.IndexOnFloor ?? -1 );
	}

	public int GetPathCost( int row, int col )
	{
		return CollectionExtensions.GetValueOrDefault( _pathCosts, (row, col), 0 );
	}
}
