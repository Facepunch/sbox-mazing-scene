using System;

namespace Mazing;

public static class Helpers
{
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
}

