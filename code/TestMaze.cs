using System;
using Sandbox;

namespace Mazing;

public sealed class TestMaze : Component
{
	public IMazeDataView? View { get; private set; }

	[Button( "Run", "casino" )]
	public void Generate()
	{
		using var _ = Scene.Push();

		foreach ( var child in GameObject.Children )
		{
			child.Destroy();
		}

		var generator = new MazeGenerator();

		generator.AddAllChunkResources();

		View = generator.Generate( new MazeGeneratorParameters( Random.Shared.Next(), 16 ) );

		var offset = new Vector3( View.Height, View.Width ) * -24f - Vector3.Up * 192f;
		var wallModel = Model.Load( "models/wall.vmdl" );

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i < View.Width; ++i )
			{
				if ( View[j, i, Direction.North] == WallState.Closed )
				{
					var wall = new GameObject
					{
						Parent = GameObject,
						Transform = { Position = new Vector3( j * 48f, i * 48f ) + offset },
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
					};
					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = wallModel;
				}
			}
		}

		for ( var j = 0; j < View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				if ( View[j, i, Direction.West] == WallState.Closed )
				{
					var wall = new GameObject {
						Parent = GameObject,
						Transform = { Position = new Vector3( j * 48f, i * 48f - 48f ) + offset,
						Rotation = Rotation.FromYaw( 90f ) },
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
					};
					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = wallModel;
				}
			}
		}
	}
}
