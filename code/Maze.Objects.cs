using System;

namespace Mazing;

partial class Maze
{
	[Property, Group( "Prefabs" )] public GameObject KeyPrefab { get; set; } = null!;
	[Property, Group( "Prefabs" )] public GameObject ExitPrefab { get; set; } = null!;
	[Property, Group( "Prefabs" )] public GameObject PlayerPrefab { get; set; }


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

	private void SpawnObjects()
	{
		_playerSpawns.Clear();
		_spawnedObjects.Clear();

		foreach ( var (row, col, state) in View!.Cells )
		{
			switch ( state )
			{
				case CellState.Key:
					_spawnedObjects.Add( KeyPrefab.Clone( MazeToWorldPos( row, col ) ) );
					break;

				case CellState.Exit:
					_spawnedObjects.Add( ExitPrefab.Clone( MazeToWorldPos( row, col ), Rotation.FromYaw( 180f ) ) );
					break;

				case CellState.Player:
					_playerSpawns.Add( (row, col) );
					break;
			}
		}

		_playerSpawns.Shuffle( Random.Shared );

		foreach ( var go in _spawnedObjects )
		{
			go.Flags |= GameObjectFlags.NotSaved;
		}

		var pawns = Scene.Components
			.GetAll<Mazer>( FindMode.InChildren | FindMode.Enabled )
			.ToArray();

		var index = 0;

		foreach ( var connection in Connection.All )
		{
			var spawn = PlayerSpawns[index++ % PlayerSpawns.Count];
			var spawnPos = MazeToWorldPos( spawn.Row, spawn.Col ) + Vector3.Up * (512f + index * 64f);

			if ( pawns.FirstOrDefault( x => x.Network.OwnerConnection == connection ) is { } pawn )
			{
				pawn.Transform.Position = spawnPos;
				pawn.State = MazerState.Falling;
			}
			else
			{
				SpawnPlayer( connection, spawnPos );
			}
		}
	}

	public void SpawnPlayer( Connection connection, Vector3? pos = null )
	{
		if ( PlayerSpawns.Count == 0 )
		{
			// No maze yet!
			return;
		}

		pos ??= MazeToWorldPos( PlayerSpawns[0].Row, PlayerSpawns[0].Col ) + Vector3.Up * 512f;

		// Spawn a player for this client
		var player = PlayerPrefab.Clone( pos.Value );

		// Spawn it on the network, assign connection as the owner
		player.NetworkSpawn( connection );
	}
}

