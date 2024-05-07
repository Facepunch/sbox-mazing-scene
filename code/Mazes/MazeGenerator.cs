using System;
using Sandbox.Diagnostics;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size );
public record struct MazeLight( Vector3 Position, Color Color, float Radius );
public record GeneratedMaze( IMazeDataView View, IReadOnlyList<MazeLight> Lights );

public partial class MazeGenerator
{
	public GeneratedMaze Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var (data, blocks) = GenerateLayout( parameters.Size, random );

		FixConnectivity( data, random );

		var lights = GenerateLights( blocks, random );

		return new GeneratedMaze( data, lights );
	}
}

