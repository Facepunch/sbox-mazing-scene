using System;

namespace Mazing;

public sealed class TestMaze : Component
{
	[Property, Group("Parameters")]
	public int Seed { get; set; } = 0x12345678;

	[Property, Group( "Parameters" )]
	public int Size { get; set; } = 4;

	[Button( "Run", "casino" ), Group( "Parameters" )]
	public void Randomize()
	{
		Seed = Random.Shared.Next();
		Generate();
	}

	[Property, Group( "Assets" )]
	public Model WallModel { get; set; }

	[Property, Group( "Assets" )]
	public Model PostModel { get; set; }

	[Property, Group( "Assets" )]
	public Model CubeModel { get; set; }

	[Property, Group( "Assets" )]
	public Material FloorMaterial { get; set; }

	public IMazeDataView? View { get; private set; }

	public void Generate()
	{
		using var _ = Scene.Push();

		foreach ( var child in GameObject.Children )
		{
			child.Destroy();
		}

		var generator = new MazeGenerator();

		generator.AddAllChunkResources();

		var result = generator.Generate( new MazeGeneratorParameters( Seed, Size ) );

		View = result.View;

		var offset = new Vector3( View.Height, View.Width ) * -24f;
		var vOffset = -Vector3.Up * 192f;

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
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
					};
					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = WallModel;
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
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
					};
					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = WallModel;
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
					Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
				};

				var renderer = post.Components.Create<ModelRenderer>();

				renderer.Model = PostModel;
			}
		}

		var empty = new List<(int Row, int Col)>();
		var populated = new HashSet<(int Row, int Col)>();

		for ( var j = -4; j <= View.Height; j += 4 )
		{
			for ( var i = -4; i <= View.Width; i += 4 )
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
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.Tint = Color.Black;

					continue;
				}

				populated.Add( (j, i) );

				{
					var floor = new GameObject
					{
						Name = "Floor",
						Parent = GameObject,
						Transform = {
							Position = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) + offset - Vector3.Up * 25f,
							LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 1f )
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
					};

					var renderer = floor.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.MaterialOverride = FloorMaterial;
				}

				foreach ( var (l, k) in empty )
				{
					var block = new GameObject
					{
						Name = "Block",
						Parent = GameObject,
						Transform = {
							Position = new Vector3( (j + l) * 48f, (i + k) * 48f ) + offset + new Vector3( 24f, 24f, 30f ),
							LocalScale = new Vector3( 0.96f, 0.96f, 1.2f )
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.Tint = Color.Black;
				}
			}
		}

		foreach ( var (pos, color, radius) in result.Lights )
		{
			var light = new GameObject
			{
				Name = "Light",
				Parent = GameObject,
				Transform = { Position = pos * 48f + offset },
				Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden
			};

			var l = light.Components.Create<PointLight>();

			l.LightColor = color;
			l.Radius = radius * 48f;
			l.Shadows = false;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		Generate();
	}
}
