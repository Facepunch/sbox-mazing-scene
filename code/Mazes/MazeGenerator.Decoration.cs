using System;

namespace Mazing;

partial class BaseMazeGenerator
{
	protected IReadOnlyList<MazeLight> GenerateLights( IMazeDataView data, Random random )
	{
		var mainHue = random.NextSingle() * 360f;

		var lightColors = new[]
		{
			new ColorHsv( mainHue, 0.5f, 4f ),
			new ColorHsv( mainHue - 30f, 0.5f, 4f ),
			new ColorHsv( mainHue + 30f, 0.5f, 4f )
		};

		const int blockSize = 6;

		var blocks = data.Cells
			.Select( x => (Row: x.Row / blockSize, Col: x.Col / blockSize) )
			.Distinct()
			.ToArray();

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
				new Vector3( (j + 0.5f) * blockSize, (i + 0.5f) * blockSize, 3f ) + random.VectorInSphere() * blockSize * 0.25f,
				lightColors[random.Next( lightColors.Length )].WithValue( 1f + random.NextSingle() * 2f ),
				blockSize * 1.5f )  );
		}

		return lights;
	}
}
