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

	[Property, Group( "Assets" )] public Model WallModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Model PostModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Model CubeModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Material FloorMaterial { get; set; } = null!;

	public IMazeDataView? View { get; private set; }

	public Vector2 WorldToMazePos( Vector3 pos )
	{
		return Transform.World.PointToLocal( pos ) / 48f;
	}

	public Vector3 MazeToWorldPos( Vector2 pos )
	{
		return Transform.World.PointToWorld( pos * 48f );
	}

	public Rect GetCellWorldRect( int row, int col )
	{
		var min = Transform.World.PointToWorld( new Vector3( row, col ) * 48f );
		var max = Transform.World.PointToWorld( new Vector3( row + 1, col + 1 ) * 48f );

		return new Rect( min, max - min );
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

		var result = generator.Generate( new MazeGeneratorParameters( Seed, Size ) );

		View = result.View;

		var vOffset = -Vector3.Up * 220f;

		const GameObjectFlags flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

		var outerOffset = Vector3.Up * 32f;

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i < View.Width; ++i )
			{
				if ( View[j, i, Direction.North] == WallState.Closed )
				{
					var outer = View[j - 1, i] == CellState.Empty || View[j, i] == CellState.Empty;

					var wall = new GameObject
					{
						Name = "Wall",
						Parent = GameObject,
						Transform = { LocalPosition = new Vector3( j * 48f, i * 48f + 48f ) + vOffset + (outer ? outerOffset : 0f) },
						Flags = flags
					};

					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = WallModel;

					var collider = wall.Components.Create<BoxCollider>();

					collider.Scale = new Vector3( 8f, 56f, 256f );
					collider.Center = new Vector3( 0f, -24f, 128f );
				}
			}
		}

		for ( var j = 0; j < View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				if ( View[j, i, Direction.West] == WallState.Closed )
				{
					var outer = View[j, i - 1] == CellState.Empty || View[j, i] == CellState.Empty;

					var wall = new GameObject
					{
						Name = "Wall",
						Parent = GameObject,
						Transform =
						{
							LocalPosition = new Vector3( j * 48f, i * 48f ) + vOffset + (outer ? outerOffset : 0f) ,
							LocalRotation = Rotation.FromYaw( 90f )
						},
						Flags = flags
					};

					var renderer = wall.Components.Create<ModelRenderer>();

					renderer.Model = WallModel;

					var collider = wall.Components.Create<BoxCollider>();

					collider.Scale = new Vector3( 8f, 56f, 256f );
					collider.Center = new Vector3( 0f, -24f, 128f );
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

				var outer = View[j - 1, i - 1] == CellState.Empty
					|| View[j - 1, i] == CellState.Empty
					|| View[j, i - 1] == CellState.Empty
					|| View[j, i] == CellState.Empty;

				var post = new GameObject
				{
					Name = "Post",
					Parent = GameObject,
					Transform = { LocalPosition = new Vector3( j * 48f, i * 48f ) + vOffset + (outer ? outerOffset : 0f) },
					Flags = flags
				};

				var renderer = post.Components.Create<ModelRenderer>();

				renderer.Model = PostModel;
			}
		}

		var empty = new List<(int Row, int Col)>();

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
							LocalPosition = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) + vOffset + new Vector3( 0f, 0f, 124f ) + outerOffset,
							LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 5.12f )
						},
						Flags = flags
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.Tint = Color.Black;

					var collider = block.Components.Create<BoxCollider>();

					continue;
				}

				{
					var floor = new GameObject
					{
						Name = "Floor",
						Parent = GameObject,
						Transform = {
							LocalPosition = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) - Vector3.Up * 25f,
							LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 1f )
						},
						Flags = flags
					};

					var renderer = floor.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.MaterialOverride = FloorMaterial;

					var collider = floor.Components.Create<BoxCollider>();
				}

				foreach ( var (l, k) in empty )
				{
					var block = new GameObject
					{
						Name = "Block",
						Parent = GameObject,
						Transform = {
							LocalPosition = new Vector3( (j + l) * 48f, (i + k) * 48f ) + vOffset + new Vector3( 24f, 24f, 124f ) + outerOffset,
							LocalScale = new Vector3( 0.96f, 0.96f, 5.12f )
						},
						Flags = flags
					};

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.Tint = Color.Black;

					var collider = block.Components.Create<BoxCollider>();
				}
			}
		}

		foreach ( var (pos, color, radius) in result.Lights )
		{
			var light = new GameObject
			{
				Name = "Light",
				Parent = GameObject,
				Transform = { LocalPosition = pos * 48f },
				Flags = flags
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
