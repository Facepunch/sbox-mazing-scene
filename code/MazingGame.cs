﻿using Sandbox.Network;
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
	[Property]
	public GameState State { get; set; }

	[Property]
	public TimeSince StateStart { get; set; }

	[Property]
	public int FirstMazeSize { get; set; } = 4;

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

		foreach ( var mazer in Scene.Components.GetAll<Mazer>( FindMode.Enabled | FindMode.InChildren ) )
		{
			anyPlayers = true;
			anyInMaze |= !mazer.IsExiting;
			anyExiting |= mazer.IsExiting && mazer.State != MazerState.Exited;
		}

		if ( !anyPlayers )
		{
			return;
		}

		if ( !anyInMaze && !anyExiting )
		{
			State = GameState.EndingLevel;
			StateStart = 0f;
		}
	}

	private void OnEndingLevel()
	{
		if ( StateStart > 2f )
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
		if ( StateStart > 2f )
		{
			State = GameState.Playing;
			StateStart = 0f;
		}
	}
}
