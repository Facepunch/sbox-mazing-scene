namespace Mazing;

partial class Maze
{
	[Property, Group( "Prefabs" )] public GameObject KeyPrefab { get; set; } = null!;
	[Property, Group( "Prefabs" )] public GameObject ExitPrefab { get; set; } = null!;

	private readonly List<(int Row, int Col)> _playerSpawns = new ();

	public IReadOnlyList<(int Row, int Col)> PlayerSpawns => _playerSpawns;
	private void SpawnObjects()
	{
		_playerSpawns.Clear();

		foreach ( var (row, col, state) in View!.Cells )
		{
			switch ( state )
			{
				case CellState.Key:
					KeyPrefab.Clone( new Transform
					{
						Position = MazeToWorldPos( row, col ),
						Rotation = Rotation.Identity,
						Scale = Vector3.One
					} );
					break;

				case CellState.Player:
					_playerSpawns.Add( (row, col) );
					break;
			}
		}
	}
}

