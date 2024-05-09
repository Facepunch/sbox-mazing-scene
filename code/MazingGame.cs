using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Mazing;

public enum GameState
{
	StartingLevel,
	Playing,
	EndingLevel
}

public class MazingGame : Component, Component.INetworkListener
{
	[Property, Sync]
	public GameState State { get; set; }

	[Property, Sync]
	public TimeSince StateStart { get; set; }

	[Property]
	public int FirstMazeSize { get; set; } = 4;

	[Property]
	public event Action? LevelCompleted;

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
		Maze.SpawnPlayer( connection );
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

			case GameState.EndingLevel:
				OnEndingLevel();
				break;
		}
	}

	private void OnPlaying()
	{
		var anyPlayers = false;
		var anyInMaze = false;
		var anyExiting = false;

		foreach ( var player in Scene.Components.GetAll<Player>( FindMode.Enabled | FindMode.InChildren ) )
		{
			anyPlayers = true;
			anyInMaze |= !player.IsExiting;
			anyExiting |= player is { IsExiting: true, HasExited: false };
		}

		if ( !anyPlayers )
		{
			return;
		}

		if ( !anyInMaze && !anyExiting )
		{
			State = GameState.EndingLevel;
			StateStart = 0f;

			DispatchLevelCompleted();
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void DispatchLevelCompleted()
	{
		LevelCompleted?.Invoke();
	}

	private void OnEndingLevel()
	{
		if ( StateStart > 1f )
		{
			if ( Maze.IsLobby )
			{
				Maze.NextLevel( FirstMazeSize, Random.Shared.Next() );
			}
			else
			{
				Maze.NextLevel( Maze.Size + 1, Random.Shared.Next() );
			}

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
