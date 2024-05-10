using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Mazing;

public enum GameState
{
	StartingLevel,
	Playing,
	Victory,
	GameOver
}

public class MazingGame : Component, Component.INetworkListener
{
	[Property, Sync]
	public GameState State { get; set; }

	[Property, Sync]
	public TimeSince StateStart { get; set; }

	[Property]
	public int InitialMazeSize { get; set; } = 4;

	[Property]
	public int InitialEnemyCount { get; set; } = 2;

	[Property]
	public int EnemyCountIncrement { get; set; } = 1;

	[Property]
	public int InitialTreasureCount { get; set; } = 2;

	[Property]
	public int TreasureCountIncrement { get; set; } = 1;

	[Property]
	public int MazeSizeIncrement { get; set; } = 1;

	[Property, Sync]
	public int Level { get; set; } = 0;

	[Property]
	public event Action? LevelCompleted;

	[Property]
	public event Action? GameOver;

	public Maze Maze => Scene.Components.Get<Maze>( FindMode.Enabled | FindMode.InChildren )
		?? throw new Exception( "No maze!" );

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";

			await Task.DelayRealtimeSeconds( 0.1f );

			GameNetworkSystem.CreateLobby();
		}
	}

	public void OnActive( Connection connection )
	{
		Maze.SpawnPlayer( connection, Level == 0 );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		switch ( State )
		{
			case GameState.StartingLevel:
				OnStartingLevel();
				break;

			case GameState.Playing:
				OnPlaying();
				break;

			case GameState.Victory:
				OnVictory();
				break;

			case GameState.GameOver:
				OnGameOver();
				break;
		}
	}

	private void OnPlaying()
	{
		var anyPlayers = false;
		var anyInMaze = false;
		var anyExiting = false;
		var anyEscaped = false;

		foreach ( var player in Scene.Components.GetAll<Player>( FindMode.Enabled | FindMode.InChildren ).ToArray() )
		{
			if ( !player.IsValid() )
			{
				continue;
			}

			anyPlayers = true;
			anyInMaze |= !player.Mazer.Throwable.IsExiting && !player.IsDead;
			anyEscaped |= player.HasExited;
			anyExiting |= player is { Mazer.Throwable.IsExiting: true, HasExited: false };

			if ( Level == 0 && player.IsDead && player.DeathTime > 5f )
			{
				var spawn = Maze.PlayerSpawns[Random.Shared.Next( Maze.PlayerSpawns.Count )];

				player.Respawn( Maze.MazeToWorldPos( spawn.Row, spawn.Col ) + Vector3.Up * 1024f );
			}
		}

		if ( !anyPlayers )
		{
			return;
		}

		if ( !anyInMaze && !anyExiting )
		{
			if ( anyEscaped )
			{
				StateStart = 0f;
				State = GameState.Victory;
				DispatchLevelCompleted();
			}
			else if ( Level > 0 )
			{
				StateStart = -2f;
				State = GameState.GameOver;
				DispatchGameOver();
			}
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void DispatchLevelCompleted()
	{
		LevelCompleted?.Invoke();
	}

	[Broadcast( NetPermission.HostOnly )]
	public void DispatchGameOver()
	{
		GameOver?.Invoke();
	}

	private void OnVictory()
	{
		if ( StateStart > 1f )
		{
			Level += 1;

			Maze.NextLevel( Level,
				InitialMazeSize + (Level - 1) * MazeSizeIncrement,
				InitialEnemyCount + (Level - 1) * EnemyCountIncrement,
				InitialTreasureCount + (Level - 1) * TreasureCountIncrement,
				Random.Shared.Next() );

			State = GameState.StartingLevel;
			StateStart = 0f;
		}
	}

	private void OnGameOver()
	{
		if ( StateStart > 1f )
		{
			Level = 0;
			Maze.NextLevel( Level, 4, 1, 2, Random.Shared.Next() );

			State = GameState.StartingLevel;
			StateStart = 0f;
		}
	}

	private void OnStartingLevel()
	{
		if ( StateStart > 1f )
		{
			State = GameState.Playing;
			StateStart = 0f;
		}
	}
}
