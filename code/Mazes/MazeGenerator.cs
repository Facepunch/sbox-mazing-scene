using System;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size, int TreasureCount, int EnemyCount );
public record struct MazeLight( Vector3 Position, Color Color, float Radius );
public record GeneratedMaze( IMazeDataView View, IReadOnlyList<MazeLight> Lights );

public interface IMazeGenerator
{
	GeneratedMaze Generate( MazeGeneratorParameters parameters );
}

public abstract partial class BaseMazeGenerator : IMazeGenerator
{
	public abstract GeneratedMaze Generate( MazeGeneratorParameters parameters );
}

public partial class MazeGenerator : BaseMazeGenerator
{
	public MazeGenerator()
	{
		AddAllChunkResources();
	}

	public override GeneratedMaze Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var data = GenerateLayout( parameters.Size, random );

		FixConnectivity( data, random );
		PlaceSpawns( data, parameters.TreasureCount, parameters.EnemyCount, random );

		var lights = GenerateLights( data, random );

		return new GeneratedMaze( data, lights );
	}
}

public sealed class LobbyMazeGenerator : BaseMazeGenerator
{
	public override GeneratedMaze Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var chunks = ResourceLibrary.GetAll<MazeChunkResource>()
			.Where( x => (x.Flags & MazeChunkFlags.IsLobby) != 0 )
			.ToArray();

		var chunk = chunks[random.Next( chunks.Length )];

		var data = new MazeData( chunk.Data.Width, chunk.Data.Height,
			chunk.Data.VertWalls.ToArray(),
			chunk.Data.HorzWalls.ToArray(),
			chunk.Data.Cells.ToArray() );

		PlaceSpawns( data, parameters.TreasureCount, parameters.EnemyCount, random );

		var lights = GenerateLights( data, random );

		return new GeneratedMaze( data, lights );
	}
}
