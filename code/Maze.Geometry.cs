﻿using System.Linq;

namespace Mazing;

partial class Maze
{
	[Property, Group( "Assets" )] public Model WallModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Model PostModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Model CubeModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Model TileModel { get; set; } = null!;
	[Property, Group( "Assets" )] public Material FloorMaterial { get; set; } = null!;

	private void UpdateGeometry( GeneratedMaze result )
	{
		var vOffset = -Vector3.Up * 220f;

		const GameObjectFlags flags = GameObjectFlags.NotNetworked | GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

		var outerOffset = Vector3.Up * 32f;
		var floorless = result.View
			.Cells
			.Where( x => x.State == CellState.Exit )
			.Select( x => (x.Row, x.Col) )
			.Union( result.Spawns
				.OfType<MazeEnemySpawn>()
				.Where( x => x.Info.IsFloorTrap )
				.SelectMany( x =>
				{
					var neighbor = x.Direction.GetNeighbor( x.Row, x.Col );
					return new [] { (x.Row, x.Col), neighbor };
				} )
			).ToHashSet();

		void CheckForWall( int j, int i, Direction dir, Vector3 offset, float yaw )
		{
			var neighbor = dir.GetNeighbor( j, i );
			var isWall = View[j, i, dir] == WallState.Closed;

			if ( isWall || floorless.Contains( (j, i) ) != floorless.Contains( neighbor ) )
			{
				var outer = View[neighbor.Row, neighbor.Col] == CellState.Empty || View[j, i] == CellState.Empty;
				var floor = !isWall && (floorless.Contains( (j, i) ) || floorless.Contains( neighbor ));

				var wall = new GameObject
				{
					Name = "Wall",
					Parent = GameObject,
					LocalPosition = new Vector3( j * 48f, i * 48f ) + offset + vOffset + (outer ? outerOffset : floor ? Vector3.Up * -38f : 0f),
					LocalRotation = Rotation.FromYaw( yaw ),
					Flags = flags
				};

				wall.Tags.Add( "wall" );

				var renderer = wall.Components.Create<ModelRenderer>();

				renderer.Model = WallModel;

				if ( !floor )
				{
					var collider = wall.Components.Create<BoxCollider>();

					collider.Scale = new Vector3( 8f, 56f, 256f );
					collider.Center = new Vector3( 0f, -24f, 128f );
					collider.Static = true;
				}
			}
		}

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i < View.Width; ++i )
			{
				CheckForWall( j, i, Direction.North, new Vector3( 0f, 48f, 0f ), 0f );
			}
		}

		for ( var j = 0; j < View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				CheckForWall( j, i, Direction.West, 0f, 90f );
			}
		}

		for ( var j = 0; j <= View.Height; ++j )
		{
			for ( var i = 0; i <= View.Width; ++i )
			{
				var west = View[j, i - 1, Direction.North] == WallState.Closed;
				var east = View[j, i, Direction.North] == WallState.Closed;
				var north = View[j - 1, i, Direction.West] == WallState.Closed;
				var south = View[j, i, Direction.West] == WallState.Closed;

				if ( !west && !east && !north && !south )
				{
					continue;
				}

				if ( west && east && !north && !south )
				{
					continue;
				}

				if ( !west && !east && north && south )
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
					LocalPosition = new Vector3( j * 48f, i * 48f ) + vOffset + (outer ? outerOffset : 0f),
					Flags = flags
				};

				var renderer = post.Components.Create<ModelRenderer>();

				renderer.Model = PostModel;
			}
		}

		var empty = new List<(int Row, int Col)>();
		var tiles = new List<(int Row, int Col)>();

		for ( var j = -4; j <= View.Height; j += 4 )
		{
			for ( var i = -4; i <= View.Width; i += 4 )
			{
				var outOfBounds = i < 0 || j < 0 || i >= View.Width || j >= View.Height;

				tiles.Clear();
				empty.Clear();

				if ( !outOfBounds )
				{
					for ( var l = 0; l < 4; ++l )
					{
						for ( var k = 0; k < 4; ++k )
						{
							if ( View[j + l, i + k] == CellState.Empty )
							{
								empty.Add( (l, k) );
								continue;
							}

							if ( !floorless.Contains( (j + l, i + k) ) )
							{
								tiles.Add( (l, k) );
							}
						}
					}
				}

				if ( tiles.Count == 16 )
				{
					var floor = new GameObject
					{
						Name = "Floor",
						Parent = GameObject,
						LocalPosition = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) - Vector3.Up * 25f,
						LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 1f ),
						Flags = flags
					};

					floor.Tags.Add( "solid" );

					var renderer = floor.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.MaterialOverride = FloorMaterial;

					var collider = floor.Components.Create<BoxCollider>();

					collider.Scale = new Vector3( 48f * 4f, 48f * 4f, 32f );
					collider.Center = new Vector3( 0f, 0f, -16f + 25f );
					collider.Static = true;
				}
				else
				{
					foreach ( var (l, k) in tiles )
					{
						var floor = new GameObject
						{
							Name = "Floor",
							Parent = GameObject,
							LocalPosition = new Vector3( (j + l + 0.5f) * 48f, (i + k + 0.5f) * 48f ),
							Flags = flags
						};

						floor.Tags.Add( "solid" );

						var renderer = floor.Components.Create<ModelRenderer>();

						renderer.Model = TileModel;

						var collider = floor.Components.Create<BoxCollider>();

						collider.Scale = new Vector3( 48f, 48f, 32f );
						collider.Center = new Vector3( 0f, 0f, -16f );
						collider.Static = true;
					}
				}

				if ( outOfBounds || empty.Count == 16 )
				{
					var block = new GameObject
					{
						Name = "Block",
						Parent = GameObject,
						LocalPosition = new Vector3( (j + 2) * 48f, (i + 2) * 48f ) + vOffset + new Vector3( 0f, 0f, 124f ) + outerOffset,
						LocalScale = new Vector3( 0.96f * 4f, 0.96f * 4f, 5.12f ),
						Flags = flags
					};

					block.Tags.Add( "solid" );

					var renderer = block.Components.Create<ModelRenderer>();

					renderer.Model = CubeModel;
					renderer.Tint = Color.Black;

					var collider = block.Components.Create<BoxCollider>();

					collider.Static = true;
				}
				else
				{
					foreach ( var (l, k) in empty )
					{
						var block = new GameObject
						{
							Name = "Block",
							Parent = GameObject,
							LocalPosition = new Vector3( (j + l) * 48f, (i + k) * 48f ) + vOffset + new Vector3( 24f, 24f, 124f ) + outerOffset,
							LocalScale = new Vector3( 0.96f, 0.96f, 5.12f ),
							Flags = flags
						};

						block.Tags.Add( "solid" );

						var renderer = block.Components.Create<ModelRenderer>();

						renderer.Model = CubeModel;
						renderer.Tint = Color.Black;

						var collider = block.Components.Create<BoxCollider>();

						collider.Static = true;
					}
				}
			}
		}

		foreach ( var (pos, color, radius) in result.Lights )
		{
			var light = new GameObject
			{
				Name = "Light",
				Parent = GameObject,
				LocalPosition = pos * 48f,
				Flags = flags
			};

			var l = light.Components.Create<PointLight>();

			l.LightColor = color;
			l.Radius = radius * 48f;
			l.Shadows = false;
		}
	}
}
