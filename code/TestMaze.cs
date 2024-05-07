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

		var offset = new Vector3( View.Height, View.Width ) * -24f;
		var vOffset = -Vector3.Up * 192f;
		var wallModel = Model.Load( "models/wall.vmdl" );
		var postModel = Model.Load( "models/post.vmdl" );
		var blockModel = Model.Load( "models/dev/box.vmdl" );

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i < View.Width; ++i )
			{
				if ( View[j, i, Direction.North] == WallState.Closed )
				{
					var wall = new GameObject
					{
						Name = "Wall",
						Parent = GameObject,
						Transform = { Position = new Vector3( j * 48f, i * 48f + 48f ) + offset + vOffset },
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
					var wall = new GameObject
					{
						Name = "Wall",
						Parent = GameObject,
						Transform =
						{
							Position = new Vector3( j * 48f, i * 48f ) + offset + vOffset,
							Rotation = Rotation.FromYaw( 90f )
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
					};
					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = wallModel;
				}
			}
		}

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				var west = View[j, i - 1, Direction.North];
				var east = View[j, i, Direction.North];
				var north = View[j - 1, i, Direction.West];
				var south = View[j, i, Direction.West];

				if ( west == east && north == south )
				{
					continue;
				}

				var post = new GameObject
				{
					Name = "Post",
					Parent = GameObject,
					Transform = { Position = new Vector3( j * 48f, i * 48f ) + offset + vOffset },
					Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
				};

				var renderer = post.Components.Create<ModelRenderer>();

				renderer.Model = postModel;
			}
		}

		var empty = new List<(int Row, int Col)>();
		var populated = new HashSet<(int Row, int Col)>();

		for ( var j = -8; j <= View.Height + 4; j += 4 )
		{
			for ( var i = -8; i <= View.Width + 4; i += 4 )
			{
				var outOfBounds = i < 0 || j < 0 || i >= View.Width || j >= View.Height;

				if ( !outOfBounds )
				{
					empty.Clear();

					for ( var l = 0; l < 4; ++l )
					{
						for ( var k = 0; k < 4; ++k )
						{
							if ( View[j + l, i + k] == CellState.Empty )
							{
								empty.Add( (l, k) );
							}
						}
					}
				}

				if ( outOfBounds || empty.Count == 16 )
				{
					var block = new GameObject
					{
						Name = "Block",
						Parent = GameObject,
						Transform = {
							Position = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) + offset + new Vector3( 0f, 0f, 30f ),
							LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 1.2f )
						}
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = blockModel;
					renderer.Tint = Color.Black;

					continue;
				}

				populated.Add( (j, i) );

				foreach ( var (l, k) in empty )
				{
					var block = new GameObject
					{
						Name = "Block",
						Parent = GameObject,
						Transform = {
							Position = new Vector3( (j + l) * 48f, (i + k) * 48f ) + offset + new Vector3( 24f, 24f, 30f ),
							LocalScale = new Vector3( 0.96f, 0.96f, 1.2f )
						}
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = blockModel;
					renderer.Tint = Color.Black;
				}
			}
		}

		var mainHue = Random.Shared.NextSingle() * 360f;

		var lightColors = new[]
		{
			new ColorHsv( mainHue, 0.5f, 4f ),
			new ColorHsv( mainHue - 30f, 0.5f, 4f ),
			new ColorHsv( mainHue + 30f, 0.5f, 4f )
		};

		var lightCoords = populated
			.SelectMany( x => new[]
			{
				(x.Row, x.Col), (x.Row - 4, x.Col), (x.Row + 4, x.Col), (x.Row, x.Col - 4), (x.Row, x.Col + 4)
			} )
			.Distinct();

		foreach ( var (j, i) in lightCoords )
		{
			var light = new GameObject
			{
				Name = "Light",
				Parent = GameObject,
				Transform = { Position = new Vector3( (j + 2) * 48f, (i + 2) * 48f, 192f ) + offset + Vector3.Random * 48f }
			};

			var l = light.Components.Create<PointLight>();

			l.LightColor = lightColors[Random.Shared.Next( lightColors.Length )];
			l.Radius = 48f * 8;
			l.Shadows = false;
		}
	}
}
