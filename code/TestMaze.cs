using System;
using Sandbox;

namespace Mazing;

public sealed class TestMaze : Component
{
	public IMazeDataView? View { get; private set; }

	[Button( "Run", "casino" )]
	public void Generate()
	{
		var generator = new MazeGenerator();
		View = generator.Generate( new MazeGeneratorParameters( Random.Shared.Next(), 16 ) );
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( View is null )
		{
			return;
		}

		var offset = new Vector3( View.Height, View.Width ) * -32f;

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i < View.Width; ++i )
			{
				if ( View[j, i, Direction.North] == WallState.Closed )
				{
					Gizmo.Draw.SolidBox( new BBox( offset + new Vector3( j * 64f, i * 64f ), offset + new Vector3( j * 64f, i * 64f + 64f, 128f ) ).Grow( 8f ) );
				}
			}
		}

		for ( var j = 0; j < View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				if ( View[j, i, Direction.West] == WallState.Closed )
				{
					Gizmo.Draw.SolidBox( new BBox( offset + new Vector3( j * 64f, i * 64f ), offset + new Vector3( j * 64f + 64f, i * 64f, 128f ) ).Grow( 8f ) );
				}
			}
		}
	}
}
