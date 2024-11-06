using System;
using Sandbox;

namespace Mazing;

public sealed class ProximityGhost : Component
{
	[RequireComponent]
	public MazeObject MazeObject { get; set; } = null!;

	[RequireComponent]
	public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[Property] public float MaxAlphaRadius { get; set; } = 0f;
	[Property] public float MinAlphaRadius { get; set; } = 180f;
	[Property] public float MaxAlpha { get; set; } = 1f;
	[Property] public float MinAlpha { get; set; } = 0f;

	private float _prevAlpha = -1f;
	private Color _baseColor;
	private readonly List<Renderer> _renderers = new();

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_baseColor = ModelRenderer.Tint;

		_renderers.Clear();
		_renderers.AddRange( Components.GetAll<Renderer>( FindMode.EverythingInSelfAndDescendants ) );
	}

	protected override void OnUpdate()
	{
		var pos = WorldPosition;

		var minDist2 = float.PositiveInfinity;

		foreach ( var player in MazeObject.Maze.Players )
		{
			if ( player.IsDead || player.HasExited )
			{
				continue;
			}

			minDist2 = Math.Min( minDist2, (player.WorldPosition - pos).LengthSquared );
		}

		var t = Math.Clamp( (MathF.Sqrt( minDist2 ) - MaxAlphaRadius) / (MinAlphaRadius - MaxAlphaRadius), 0f, 1f );
		var alpha = MaxAlpha - (MaxAlpha - MinAlpha) * t;

		alpha = Math.Max( _prevAlpha - Time.Delta, alpha );

		if ( MathF.Abs( alpha - _prevAlpha ) < 0.01f )
		{
			return;
		}

		_prevAlpha = alpha;

		foreach ( var renderer in _renderers )
		{
			if ( renderer is ITintable tintable )
			{
				tintable.Color = _baseColor.WithAlpha( alpha );
			}

			renderer.Enabled = alpha >= 0.01f;
		}
	}
}
