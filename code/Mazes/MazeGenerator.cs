using System;
using Sandbox.Diagnostics;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size );

public partial class MazeGenerator
{
	public IMazeDataView Generate( MazeGeneratorParameters parameters )
	{
		var random = new Random( parameters.Seed );
		var data = GenerateLayout( parameters.Size, random );

		FixConnectivity( data, random );

		return data;
	}
}

