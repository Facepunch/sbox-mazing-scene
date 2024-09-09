using System;

namespace Mazing;

partial class Maze
{
	[Property, Group( "Prefabs" )] public GameObject KeyPrefab { get; set; } = null!;
	[Property, Group( "Prefabs" )] public GameObject ExitPrefab { get; set; } = null!;
	[Property, Group( "Prefabs" )] public GameObject PlayerPrefab { get; set; } = null!;

	private readonly List<(int Row, int Col)> _playerSpawns = new ();
	private readonly List<GameObject> _spawnedObjects = new();

	public IReadOnlyList<(int Row, int Col)> PlayerSpawns => _playerSpawns;

	public void DestroySpawnedObjects()
	{
		foreach ( var go in _spawnedObjects )
		{
			go.Destroy();
		}

		_spawnedObjects.Clear();
	}

	private void SpawnObjects( GeneratedMaze result )
	{
		if ( Network.IsProxy )
		{
			return;
		}

		_playerSpawns.Clear();
		_spawnedObjects.Clear();

		foreach ( var spawn in result.Spawns )
		{
			switch ( spawn )
			{
				case MazePlayerSpawn playerSpawn:
					_playerSpawns.Add( (playerSpawn.Row, playerSpawn.Col) );
					break;

				case MazeKeySpawn keySpawn:
					_spawnedObjects.Add( KeyPrefab.Clone( MazeToWorldPos( keySpawn.Row, keySpawn.Col ) ) );
					break;

				case MazeExitSpawn exitSpawn:
					_spawnedObjects.Add( ExitPrefab.Clone( MazeToWorldPos( exitSpawn.Row, exitSpawn.Col ) ) );
					break;

				case MazeEnemySpawn enemySpawn:
					_spawnedObjects.Add(
						SceneUtility.GetPrefabScene( enemySpawn.Prefab )
							.Clone( MazeToWorldPos( enemySpawn.Row, enemySpawn.Col ), enemySpawn.Direction.GetRotation() ) );

					if ( enemySpawn.Info.IsFloorTrap )
					{
						var neighbor = enemySpawn.Direction.GetNeighbor( enemySpawn.Row, enemySpawn.Col );

						_spawnedObjects.Add(
							SceneUtility.GetPrefabScene( enemySpawn.Prefab )
								.Clone( MazeToWorldPos( neighbor.Row, neighbor.Col ), enemySpawn.Direction.GetRotation() ) );
					}

					break;

				case MazeTreasureSpawn treasureSpawn:
					_spawnedObjects.Add(
						SceneUtility.GetPrefabScene( treasureSpawn.Prefab )
							.Clone( MazeToWorldPos( treasureSpawn.Row, treasureSpawn.Col ) ) );
					break;
			}
		}

		if ( !Game.IsPlaying )
		{
			foreach ( var go in _spawnedObjects )
			{
				go.Flags |= GameObjectFlags.NotSaved;
			}

			return;
		}

		foreach ( var go in _spawnedObjects )
		{
			go.NetworkSpawn( Connection.Host );
		}

		var pawns = Players
			.ToHashSet();

		var index = 0;

		foreach ( var connection in Connection.All )
		{
			var spawn = PlayerSpawns[index++ % PlayerSpawns.Count];
			var spawnPos = MazeToWorldPos( spawn.Row, spawn.Col ) + Vector3.Up * (1024f + index * 128f);

			var pawn = pawns.FirstOrDefault( x => x.Network.OwnerConnection == connection )
				?? SpawnPlayer( connection, true, spawnPos );

			if ( pawn is not null )
			{
				pawns.Remove( pawn );
				pawn.Respawn( spawnPos );
			}
		}

		// Remove disconnected players

		foreach ( var pawn in pawns )
		{
			pawn.GameObject.Destroy();
			_players.Remove( pawn );
		}
	}

	public Player? SpawnPlayer( Connection connection, bool alive, Vector3? pos = null )
	{
		if ( PlayerSpawns.Count == 0 )
		{
			// No maze yet!
			return null;
		}

		if ( Players.FirstOrDefault( x => x.Network.OwnerConnection == connection ) is {} player )
		{
			return player;
		}

		pos ??= MazeToWorldPos( PlayerSpawns[0].Row, PlayerSpawns[0].Col ) + Vector3.Up * 512f;

		// Spawn a player for this client
		var obj = PlayerPrefab.Clone( pos.Value );

		player = obj.Components.Get<Player>();

		_players.Add( player );

		// Spawn it on the network, assign connection as the owner
		obj.NetworkSpawn( connection );

		if ( !alive )
		{
			player.Kill( Vector3.Zero, false );
		}
		else
		{
			player.Respawn( pos.Value );
		}

		return player;
	}
}
