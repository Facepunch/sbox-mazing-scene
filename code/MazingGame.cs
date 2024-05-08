using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mazing;

public class MazingGame : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }

	public void OnActive( Connection connection )
	{
		var maze = Scene.Components.Get<Maze>( FindMode.Enabled | FindMode.InChildren )
			?? throw new Exception( "No maze!" );

		// Spawn a player for this client
		var player = PlayerPrefab.Clone( maze.MazeToWorldPos( maze.PlayerSpawns[0].Row, maze.PlayerSpawns[0].Col ) );

		// Spawn it on the network, assign connection as the owner
		player.NetworkSpawn( connection );
	}
}

