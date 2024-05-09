﻿using System;

namespace Mazing;

public static class Helpers
{
	public static IReadOnlyList<Direction> Directions { get; } = new[]
	{
		Direction.West, Direction.North, Direction.East, Direction.South
	};

	public static float Ease( float frac )
	{
		return Ease( frac, Time.Delta );
	}

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

	public static void Shuffle<T>( this IList<T> list, Random random )
	{
		for ( var i = 0; i < list.Count - 1; ++i )
		{
			var j = random.Next( i, list.Count );
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}

