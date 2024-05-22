
namespace Mazing;

public sealed class Seeker : Wanderer
{
	protected override Direction? GetNewTarget()
	{
		var ends = MazeObject.Maze.Players
			.Where( x => x.Mazer.State is MazerState.Walking or MazerState.Vaulting )
			.Select( x => x.Mazer.MazeObject.CellIndex )
			.ToArray();

		var path = MazeObject.View.FindPathToAny( MazeObject.CellIndex, ends, 256, MazeObject.Maze.GetPathCost );

		return path?[0].GetDirectionTo( path[1] ) ?? base.GetNewTarget();
	}
}

