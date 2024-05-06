using System;

namespace Mazing;

/// <param name="Size">How big is the maze, in 4x4 blocks.</param>
public record struct MazeGeneratorParameters( int Seed, int Size );

public class MazeGenerator
{
	public IMazeDataView Generate( MazeGeneratorParameters parameters )
	{
		var resources = ResourceLibrary.GetAll<MazeChunkResource>()
			.Where( x => !x.IsSpecial )
			.Where( x => (x.Data.Width & 3) == 0 && (x.Data.Height & 3) == 0 )
			.ToArray();

		var chunks = resources
			.SelectMany( x => new[]
			{
				x.Data, x.Data.Transpose(), x.Data.FlipHorizontal(), x.Data.FlipHorizontal().Transpose(),
				x.Data.FlipVertical(), x.Data.FlipVertical().Transpose(),
				x.Data.FlipHorizontal().FlipVertical().Transpose()
			} )
			.ToArray();

		throw new NotImplementedException();
	}
}

