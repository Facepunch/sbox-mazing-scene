
namespace Mazing;

public sealed class Seeker : Wanderer
{
	protected override (int Row, int Col)? GetNewTarget()
	{
		var ends = Scene.Components.GetAll<Player>( FindMode.Enabled | FindMode.InChildren )
			.Where( x => x.Mazer.State is MazerState.Walking or MazerState.Vaulting )
			.Select( x => x.Mazer.MazeObject.CellIndex )
			.ToArray();

		var path = MazeObject.View.FindPathToAny( MazeObject.CellIndex, ends, 256, MazeObject.Maze.GetPathCost );

		return path?[1] ?? base.GetNewTarget();
	}
}

