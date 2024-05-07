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
		// Spawn a player for this client
		var player = PlayerPrefab.Clone( new Transform( new Vector3( 24f, 24f, 1024f ) ) );

		// Spawn it on the network, assign connection as the owner
		player.NetworkSpawn( connection );
	}
}

