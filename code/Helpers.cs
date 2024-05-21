using System;
using System.Text.Json;

namespace Mazing;

public static class Helpers
{
	public static IReadOnlyList<Direction> Directions { get; } = new[]
	{
		Direction.West, Direction.North, Direction.East, Direction.South
	};

	[Pure]
	public static float Ease( float frac )
	{
		return Ease( frac, Time.Delta );
	}

	[Pure]
	public static float Ease( float frac, float dt )
	{
		if ( float.IsPositiveInfinity( dt ) ) return 1f;

		return 1f - MathF.Pow( 1f - Math.Clamp( frac, 0f, 1f ), dt * 60f );
	}

	[ActionGraphNode( "mazing.isproxy" ), Pure]
	public static bool IsProxy( this GameObject go )
	{
		return go.Network.IsProxy;
	}

	[ActionGraphNode( "mazing.isowner" ), Pure]
	public static bool IsOwner( this GameObject go )
	{
		return go.Network.IsOwner;
	}

	[ActionGraphNode( "mazing.isvalid" ), Pure]
	public static bool Exists( this GameObject go )
	{
		return go.IsValid;
	}

	[Pure]
	public static float EaseTo( float a, float b, float frac )
	{
		return MathX.Lerp( a, b, Ease( frac ) );
	}

	[Pure]
	public static float LerpTo( float a, float b, float speed )
	{
		var diff = b - a;
		var delta = speed * Time.Delta;

		if ( Math.Abs( diff ) < delta )
		{
			return b;
		}

		return a + delta * Math.Sign( diff );
	}

	public static void Shuffle<T>( this IList<T> list, Random random )
	{
		for ( var i = 0; i < list.Count - 1; ++i )
		{
			var j = random.Next( i, list.Count );
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

	public static TInfo? DeserializeFromPrefab<TComponent, TInfo>( PrefabFile prefab )
		where TComponent : Component
		where TInfo : class
	{
		var typeName = typeof(TComponent).FullName;

		var component = prefab.RootObject?["Components"]?.AsArray()
			.FirstOrDefault( x => x?["__type"]?.GetValue<string>() == typeName );

		return component?.Deserialize<TInfo>();
	}

	public static IReadOnlyList<(PrefabFile Prefab, TInfo Info)> FindAllPrefabsWithInfo<TComponent, TInfo>()
		where TComponent : Component
		where TInfo : class
	{
		return ResourceLibrary.GetAll<PrefabFile>()
			.Select( x => (Prefab: x, Info: DeserializeFromPrefab<TComponent, TInfo>( x )!) )
			.Where( x => x.Info != null! )
			.ToArray();
	}

	public static int GetMaxRange( this IMazeDataView mazeData, int row, int col, Direction dir )
	{
		var max = Math.Max( mazeData.Width, mazeData.Height );
		var range = 0;

		for ( var i = 0; i < max; ++i )
		{
			if ( mazeData[row, col, dir] != WallState.Open )
			{
				break;
			}

			++range;
			(row, col) = dir.GetNeighbor( row, col );
		}

		return range;
	}

	public static Direction NextDirection( this Random random, IMazeDataView mazeData, int row, int col )
	{
		var scores = Directions
			.Select( x => (Direction: x, Score: GetMaxRange( mazeData, row, col, x )) )
			.Select( x => x with { Score = x.Score * x.Score } )
			.ToArray();

		var total = scores.Sum( x => x.Score );
		var selected = random.Next( 0, total );

		foreach ( var (dir, score) in scores )
		{
			selected -= score;

			if ( selected < 0 )
			{
				return dir;
			}
		}

		return (Direction)random.Next( 0, 4 );
	}
}

