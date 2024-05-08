using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Mazing;

public class MazingGame : Component, Component.INetworkListener
{
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
		var maze = Scene.Components.Get<Maze>( FindMode.Enabled | FindMode.InChildren )
			?? throw new Exception( "No maze!" );

		maze.SpawnPlayer( connection );
	}
}
