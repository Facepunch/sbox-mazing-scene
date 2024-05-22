using System;

namespace Mazing;

partial class BaseMazeGenerator
{
	private static (PrefabFile Prefab, T Info)? GetPrefab<T>( ref int remainingPoints, Queue<(PrefabFile Prefab, T Info, int Points)> queue )
	{
		if ( remainingPoints <= 0 )
		{
			return null;
		}

		while ( queue.Count > 0 )
		{
			var next = queue.Dequeue();

			if ( next.Points > remainingPoints )
			{
				continue;
			}

			remainingPoints -= next.Points;

			queue.Enqueue( next );
			return (next.Prefab, next.Info);
		}

		return null;
	}

	public IReadOnlyList<(PrefabFile Prefab, EnemyInfo Info)> GenerateEnemyList( int seed, int level, int count )
	{
		var random = new Random( seed );

		var enemyTypes = Helpers.FindAllPrefabsWithInfo<Enemy, EnemyInfo>();

		var requiredEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel == level )
			.ToArray();

		requiredEnemyTypes.Shuffle( random );

		var spareEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel < level )
			.OrderByDescending( x => x.Info.MinLevel + random.Next( 5 ) )
			.ToArray();

		var minEnemyTypeCount = Math.Max( requiredEnemyTypes.Length, 2 );
		var enemyTypeCount = Math.Min( random.Next( minEnemyTypeCount, 4 ),
			requiredEnemyTypes.Length + spareEnemyTypes.Length );

		var enemyTypeQueue = new Queue<(PrefabFile Prefab, EnemyInfo Info, int Points)>(
			requiredEnemyTypes
				.Concat( spareEnemyTypes )
				.Take( enemyTypeCount )
				.Select( x => (x.Prefab, x.Info, x.Info.Difficulty) ) );

		var enemyPoints = count;
		var result = new List<(PrefabFile Prefab, EnemyInfo Info)>();

		while ( enemyPoints > 0 && GetPrefab( ref enemyPoints, enemyTypeQueue ) is { } enemyPrefab )
		{
			result.Add( enemyPrefab );
		}

		return result;
	}

	public IReadOnlyList<(PrefabFile Prefab, TreasureInfo Info)> GenerateTreasureList( int seed, int level, int count )
	{
		var treasureTypeQueue = new Queue<(PrefabFile Prefab, TreasureInfo Info, int Points)>(
			Helpers.FindAllPrefabsWithInfo<Treasure, TreasureInfo>()
				.OrderBy( x => x.Info.Value )
				.Select( x => (x.Prefab, x.Info, x.Info.Value) ) );

		var treasurePoints = count;
		var result = new List<(PrefabFile Prefab, TreasureInfo Info)>();

		while ( treasurePoints > 0 && GetPrefab( ref treasurePoints, treasureTypeQueue ) is { } treasurePrefab )
		{
			result.Add( treasurePrefab );
		}

		return result;
	}
}
