using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mazing;

partial class BaseMazeGenerator
{
	private static PrefabFile? GetPrefab( ref int remainingPoints, Queue<(PrefabFile Prefab, int Points)> queue )
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
			return next.Prefab;
		}

		return null;
	}

	public IReadOnlyList<PrefabFile> GenerateEnemyList( int seed, int level, int count )
	{
		var random = new Random( seed );

		var enemyTypes = Helpers.FindAllPrefabsWithInfo<Enemy, EnemyInfo>();

		var requiredEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel == level )
			.ToArray();

		requiredEnemyTypes.Shuffle( random );

		var spareEnemyTypes = enemyTypes
			.Where( x => x.Info.MinLevel < level )
			.ToArray();

		spareEnemyTypes.Shuffle( random );

		var minEnemyTypeCount = Math.Max( requiredEnemyTypes.Length, random.NextSingle() < 0.25f ? 1 : 2 );
		var enemyTypeCount = Math.Min( random.Next( minEnemyTypeCount, 4 ),
			requiredEnemyTypes.Length + spareEnemyTypes.Length );

		var enemyTypeQueue = new Queue<(PrefabFile Prefab, int Points)>(
			requiredEnemyTypes
				.Concat( spareEnemyTypes )
				.Take( enemyTypeCount )
				.Select( x => (x.Prefab, x.Info.Difficulty) ) );

		var enemyPoints = count;
		var result = new List<PrefabFile>();

		while ( enemyPoints > 0 && GetPrefab( ref enemyPoints, enemyTypeQueue ) is { } enemyPrefab )
		{
			result.Add( enemyPrefab );
		}

		return result;
	}

	public IReadOnlyList<PrefabFile> GenerateTreasureList( int seed, int level, int count )
	{
		var treasureTypeQueue = new Queue<(PrefabFile Prefab, int Points)>(
			Helpers.FindAllPrefabsWithInfo<Treasure, TreasureInfo>()
				.OrderBy( x => x.Info.Value )
				.Select( x => (x.Prefab, x.Info.Value) ) );

		var treasurePoints = count;
		var result = new List<PrefabFile>();

		while ( treasurePoints > 0 && GetPrefab( ref treasurePoints, treasureTypeQueue ) is { } treasurePrefab )
		{
			result.Add( treasurePrefab );
		}

		return result;
	}
}
