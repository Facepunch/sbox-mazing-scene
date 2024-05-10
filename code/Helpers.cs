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

	[ActionGraphNode( "mazing.isowner" ), Pure]
	public static bool IsOwner( this GameObject go )
	{
		return go.Network.IsOwner;
	}

	[ActionGraphNode( "mazing.localplayer" ), Pure]
	public static Player? GetLocalPlayer( this GameObject go )
	{
		return go.Scene.Components
			.GetAll<Player>( FindMode.Enabled | FindMode.InChildren )
			.FirstOrDefault( x => x.GameObject.IsOwner() );
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
}

