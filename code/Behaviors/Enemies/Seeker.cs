
namespace Mazing;

public sealed class Seeker : Wanderer
{
	[Property]
	public bool CanVault { get; set; }

	[Property, HideIf( nameof(CanVault), false )]
	public float VaultPeriod { get; set; } = 4f;

	private TimeSince _lastVault;
	private TimeUntil _preVault;
	private bool _vaulted;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy )
		{
			return;
		}

		if ( !_vaulted && _preVault )
		{
			_vaulted = true;
			_lastVault = 0f;

			Mazer.TryVault();
		}
	}

	protected override Direction? GetNewTarget()
	{
		var ends = MazeObject.Maze.Players
			.Where( x => x.Mazer.State is MazerState.Walking or MazerState.Vaulting )
			.Select( x => x.Mazer.MazeObject.CellIndex )
			.ToArray();

		var path = MazeObject.View.FindPathToAny( MazeObject.CellIndex, ends, 256, MazeObject.Maze.GetPathCost );

		if ( path is null )
		{
			return base.GetNewTarget();
		}

		if ( !CanVault || _lastVault < VaultPeriod )
		{
			return path[0].GetDirectionTo( path[1] );
		}

		var (row, col) = MazeObject.CellIndex;

		foreach ( var direction in Helpers.Directions )
		{
			if ( MazeObject.View[row, col, direction] == WallState.Open )
			{
				continue;
			}

			var neighbor = direction.GetNeighbor( row, col );
			var vaultedPath = MazeObject.View.FindPath( neighbor, path[^1], path.Count, MazeObject.Maze.GetPathCost );

			if ( vaultedPath is not null && vaultedPath.Count < path.Count )
			{
				_vaulted = false;
				_preVault = 0.5f;

				path = vaultedPath;
			}
		}

		return path[0].GetDirectionTo( path[1] );
	}
}

