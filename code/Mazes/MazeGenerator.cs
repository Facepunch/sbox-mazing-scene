using System;
using System.Linq;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record MazeGeneratorParameters( int Seed, int Level, int Size, int TreasureCount, int EnemyCount );
public record MazeLight( Vector3 Position, Color Color, float Radius );

public record MazeObjectSpawn( int Row, int Col );
public record MazeTreasureSpawn( int Row, int Col, PrefabFile Prefab, TreasureInfo Info ) : MazeObjectSpawn( Row, Col );
public record MazeEnemySpawn( int Row, int Col, Direction Direction, PrefabFile Prefab, EnemyInfo Info ) : MazeObjectSpawn( Row, Col );
public record MazePlayerSpawn( int Row, int Col ) : MazeObjectSpawn( Row, Col );
public record MazeKeySpawn( int Row, int Col ) : MazeObjectSpawn( Row, Col );
public record MazeExitSpawn( int Row, int Col ) : MazeObjectSpawn( Row, Col );

public record GeneratedMaze(
	IMazeDataView View,
	IReadOnlyList<MazeObjectSpawn> Spawns,
	IReadOnlyList<MazeLight> Lights );

public interface IMazeGenerator
{
	GeneratedMaze Generate( MazeGeneratorParameters parameters );
}

public abstract partial class BaseMazeGenerator : IMazeGenerator
{
	public GeneratedMaze Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var layout = GenerateLayout( random.Next(), parameters.Size );
		var treasure = GenerateTreasureList( random.Next(), parameters.Level, parameters.TreasureCount );
		var enemies = GenerateEnemyList( random.Next(), parameters.Level, parameters.EnemyCount );
		var spawns = PlaceSpawns( random.Next(), layout, treasure.Count, enemies.Count );
		var lights = GenerateLights( random.Next(), layout );

		var finalSpawns = new List<MazeObjectSpawn>();

		finalSpawns.AddRange( spawns[CellState.Player].Select( x =>
			new MazePlayerSpawn( x.Row, x.Col ) ) );

		finalSpawns.AddRange( spawns[CellState.Key].Select( x =>
			new MazeKeySpawn( x.Row, x.Col ) ) );

		finalSpawns.AddRange( spawns[CellState.Exit].Select( x =>
			new MazeExitSpawn( x.Row, x.Col ) ) );

		finalSpawns.AddRange( spawns[CellState.Treasure].Select( ( x, i ) =>
			new MazeTreasureSpawn( x.Row, x.Col, treasure[i].Prefab, treasure[i].Info ) ) );

		finalSpawns.AddRange( spawns[CellState.Enemy].Select( ( x, i ) =>
			new MazeEnemySpawn( x.Row, x.Col, x.Dir, enemies[i].Prefab, enemies[i].Info ) ) );

		return new GeneratedMaze( layout, finalSpawns, lights );
	}

	protected abstract MazeData GenerateLayout( int seed, int size );
}

public partial class MazeGenerator : BaseMazeGenerator
{
	public MazeGenerator()
	{
		AddAllChunkResources();
	}
}

public sealed class LobbyMazeGenerator : BaseMazeGenerator
{
	protected override MazeData GenerateLayout( int seed, int size )
	{
		var random = new Random( seed );
		var chunks = ResourceLibrary.GetAll<MazeChunkResource>()
			.Where( x => (x.Flags & MazeChunkFlags.IsLobby) != 0 )
			.ToArray();

		var chunk = chunks[random.Next( chunks.Length )];

		return new MazeData( chunk.Data.Width, chunk.Data.Height,
			chunk.Data.VertWalls.ToArray(),
			chunk.Data.HorzWalls.ToArray(),
			chunk.Data.Cells.ToArray() );
	}
}
