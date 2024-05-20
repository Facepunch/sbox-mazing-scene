
using System;

namespace Mazing;

internal sealed class Projectile : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	[Property, Sync]
	public bool Moving { get; set; }

	[Property, Sync]
	public Direction Direction { get; set; }

	[Property]
	public float Speed { get; set; } = 160f;

	[Property]
	public float Radius { get; set; } = 4f;

	[Property]
	public bool IgnoreWalls { get; set; }

	private TimeSince _ended;

	[Property]
	public event Action<Player, Vector3>? HitPlayer;

	[Property]
	public event Action<Vector3>? HitWall;

	public void Fire( Direction direction )
	{
		Moving = true;
		Direction = direction;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchHitPlayer( Guid guid, Vector3 pos )
	{
		var player = Scene.Directory.FindComponentByGuid( guid ) as Player
			?? throw new Exception( $"Unknown player {guid}" );

		HitPlayer?.Invoke( player, pos );
	}

	protected override void OnUpdate()
	{
		if ( !Moving )
		{
			if ( !IsProxy && _ended > 2f )
			{
				GameObject.Destroy();
			}

			return;
		}

		var prevIndex = MazeObject.CellIndex;
		var direction = new Vector3( Direction.GetNormal(), 0f );
		var velocity = direction * Speed;
		var delta = velocity * Time.Delta;

		if ( !IsProxy )
		{
			var hits = Scene.Trace
				.Sphere( Radius, Transform.Position, Transform.Position + delta )
				.WithTag( "player" )
				.RunAll();

			foreach ( var hit in hits )
			{
				var player = hit.GameObject.Components.Get<Player>();

				if ( player is null || player.Mazer.State != MazerState.Walking )
				{
					continue;
				}

				DispatchHitPlayer( player.Id, Transform.Position );
				player.Kill( velocity * 50f );
			}
		}

		Transform.Position += delta;

		var nextIndex = MazeObject.CellIndex;

		if ( prevIndex != nextIndex )
		{
			var wallHit = MazeObject.View[prevIndex.Row, prevIndex.Col, Direction] != WallState.Open;
			var emptyHit = MazeObject.View[nextIndex.Row, nextIndex.Col] == CellState.Empty;

			if ( !wallHit && !emptyHit )
			{
				return;
			}

			var hitPos = ((MazeObject.Maze.MazeToWorldPos( prevIndex.Row, prevIndex.Col )
				+ MazeObject.Maze.MazeToWorldPos( nextIndex.Row, nextIndex.Col )) * 0.5f)
				.WithZ( Transform.Position.z )
				- direction * 4f;

			HitWall?.Invoke( hitPos );

			if ( !IgnoreWalls || emptyHit )
			{
				Transform.Position = hitPos;

				Moving = false;
				_ended = 0;
			}
		}
	}
}
