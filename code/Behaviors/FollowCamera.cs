using Sandbox;

namespace Mazing;

public sealed class FollowCamera : Component
{
	[RequireComponent] public ColorAdjustments ColorAdjustments { get; set; } = null!;

	[Property]
	public GameObject? Target { get; set; }

	[Property]
	public float Distance { get; set; } = 1500f;

	protected override void OnUpdate()
	{
		if ( Target is null )
		{
			return;
		}

		WorldPosition = Target.WorldPosition.WithZ( 64f ) - WorldRotation.Forward * Distance;

		var player = Target.Components.Get<Player>();

		ColorAdjustments.Saturation = Helpers.LerpTo( ColorAdjustments.Saturation, player?.IsDead is true ? 0.25f : 1f, 0.5f );
	}
}
