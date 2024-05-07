using System;

namespace Mazing;

partial class MazeGenerator
{
	private IReadOnlyList<MazeLight> GenerateLights( IReadOnlyList<(int Row, int Col)> blocks, Random random )
	{
		var mainHue = random.NextSingle() * 360f;

		var lightColors = new[]
		{
			new ColorHsv( mainHue, 0.5f, 4f ),
			new ColorHsv( mainHue - 30f, 0.5f, 4f ),
			new ColorHsv( mainHue + 30f, 0.5f, 4f )
		};

		var lightCoords = blocks
			.SelectMany( x => new[]
			{
				(x.Row, x.Col), (x.Row - 1, x.Col), (x.Row + 1, x.Col), (x.Row, x.Col - 1), (x.Row, x.Col + 1)
			} )
			.Distinct();

		var lights = new List<MazeLight>();

		foreach ( var (j, i) in lightCoords )
		{
			lights.Add( new MazeLight(
				new Vector3( j * 4 + 2, i * 4 + 2, 3 ) + random.VectorInSphere(),
				lightColors[random.Next( lightColors.Length )].WithValue( 1f + random.NextSingle() * 3f ),
				5f )  );
		}

		return lights;
	}
}
