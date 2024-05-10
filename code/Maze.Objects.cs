﻿using System;
using System.Collections;
using System.Text.Json;
using static Sandbox.PhysicsGroupDescription.BodyPart;

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

	private record EnemyInfo( int Difficulty, int MinLevel )
	{
		public static EnemyInfo? FromPrefab( PrefabFile prefab )
		{
			var component = prefab.RootObject?["Components"]?.AsArray()
				.FirstOrDefault(x => x?["__type"]?.GetValue<string>() == "Mazing.Enemy" );

			return component?.Deserialize<EnemyInfo>();
		}
	}

	private void SpawnObjects()
	{
		if ( Network.IsProxy )
		{
			return;
		}

		_playerSpawns.Clear();
		_spawnedObjects.Clear();

		var random = new Random( Seed );

		var enemyTypes = ResourceLibrary.GetAll<PrefabFile>()
			.Select( x => (Info: EnemyInfo.FromPrefab( x )!, Prefab: x) )
			.Where( x => x.Info != null! )
			.ToArray();

		var requiredEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel == Level )
			.ToArray();

		requiredEnemyTypes.Shuffle( random );

		var spareEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel < Level )
			.ToArray();

		spareEnemyTypes.Shuffle( random );

		var minEnemyTypeCount = Math.Max( requiredEnemyTypes.Length, random.NextSingle() < 0.25f ? 1 : 2 );
		var enemyTypeCount = Math.Min( random.Next( minEnemyTypeCount, 4 ),
			requiredEnemyTypes.Length + spareEnemyTypes.Length );

		var enemyTypeQueue = new Queue<(EnemyInfo Info, PrefabFile Prefab)>(
			requiredEnemyTypes
			.Concat( spareEnemyTypes )
			.Take( enemyTypeCount ) );

		var enemyPoints = EnemyCount;

		PrefabFile? GetEnemyPrefab()
		{
			if ( enemyPoints <= 0 )
			{
				return null;
			}

			while ( enemyTypeQueue.Count > 0 )
			{
				var next = enemyTypeQueue.Dequeue();

				if ( next.Info.Difficulty > enemyPoints )
				{
					continue;
				}

				enemyPoints -= next.Info.Difficulty;

				enemyTypeQueue.Enqueue( next );
				return next.Prefab;
			}

			return null;
		}

		foreach ( var (row, col, state) in View!.Cells )
		{
			switch ( state )
			{
				case CellState.Key:
					_spawnedObjects.Add( KeyPrefab.Clone( MazeToWorldPos( row, col ) ) );
					break;

				case CellState.Exit:
					_spawnedObjects.Add( ExitPrefab.Clone( MazeToWorldPos( row, col ) ) );
					break;

				case CellState.Enemy:
					if ( GetEnemyPrefab() is { } prefab )
					{
						var scene = SceneUtility.GetPrefabScene( prefab );
						_spawnedObjects.Add( scene.Clone( MazeToWorldPos( row, col ) ) );
					}
					break;

				case CellState.Player:
					_playerSpawns.Add( (row, col) );
					break;
			}
		}

		_playerSpawns.Shuffle( Random.Shared );

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

		var pawns = Scene.Components
			.GetAll<Player>( FindMode.InChildren | FindMode.Enabled )
			.ToArray();

		var index = 0;

		foreach ( var connection in Connection.All )
		{
			var spawn = PlayerSpawns[index++ % PlayerSpawns.Count];
			var spawnPos = MazeToWorldPos( spawn.Row, spawn.Col ) + Vector3.Up * (1024f + index * 128f);

			var pawn = pawns.FirstOrDefault( x => x.Network.OwnerConnection == connection )
				?? SpawnPlayer( connection, spawnPos );

			pawn?.Respawn( spawnPos );
		}
	}

	public Player? SpawnPlayer( Connection connection, Vector3? pos = null )
	{
		if ( PlayerSpawns.Count == 0 )
		{
			// No maze yet!
			return null;
		}

		pos ??= MazeToWorldPos( PlayerSpawns[0].Row, PlayerSpawns[0].Col ) + Vector3.Up * 512f;

		// Spawn a player for this client
		var player = PlayerPrefab.Clone( pos.Value );

		// Spawn it on the network, assign connection as the owner
		player.NetworkSpawn( connection );

		return player.Components.Get<Player>();
	}
}
