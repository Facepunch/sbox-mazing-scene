using System;

namespace Mazing;

public sealed class TestMaze : Component
{
	[Property]
	public int Seed { get; set; } = 0x12345678;

	[Property]
	public int Size { get; set; } = 4;

	public IMazeDataView? View { get; private set; }

	[Button( "Run", "casino" )]
	public void Randomize()
	{
		Seed = Random.Shared.Next();
		Generate();
	}

	public void Generate()
	{
		using var _ = Scene.Push();

		foreach ( var child in GameObject.Children )
		{
			child.Destroy();
		}

		var generator = new MazeGenerator();

		generator.AddAllChunkResources();

		var result = generator.Generate( new MazeGeneratorParameters( Random.Shared.Next(), Size ) );

		View = result.View;

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
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
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
						},
						Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = blockModel;
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
				Flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved
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
