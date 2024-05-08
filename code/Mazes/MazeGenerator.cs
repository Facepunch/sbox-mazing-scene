using System;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size, int TreasureCount, int EnemyCount );
public record struct MazeLight( Vector3 Position, Color Color, float Radius );
public record GeneratedMaze( IMazeDataView View, IReadOnlyList<MazeLight> Lights );

public partial class MazeGenerator
{
	public GeneratedMaze Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var (data, blocks) = GenerateLayout( parameters.Size, random );

		FixConnectivity( data, random );
		PlaceSpawns( data, parameters.TreasureCount, parameters.EnemyCount, random );

		var lights = GenerateLights( blocks, random );

		return new GeneratedMaze( data, lights );
	}
}

