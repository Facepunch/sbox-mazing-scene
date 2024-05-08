using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Mazing;

public class MazingGame : Component, Component.INetworkListener
{
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
		var anyPlayers = false;
		var anyInMaze = false;
		var anyExited = false;

		foreach ( var mazer in Scene.Components.GetAll<Mazer>( FindMode.Enabled | FindMode.InChildren ) )
		{
			anyPlayers = true;
			anyInMaze |= !mazer.IsExiting;
			anyExited |= mazer.State == MazerState.Exited;
		}

		if ( !anyPlayers )
		{
			return;
		}

		if ( !anyInMaze && anyExited )
		{
			Maze.Size += 1;
			Maze.Seed = Random.Shared.Next();
			Maze.Generate();
		}
	}
}
