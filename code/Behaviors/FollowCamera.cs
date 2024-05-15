using Sandbox;

namespace Mazing;

public sealed class FollowCamera : Component
{
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

		Transform.Position = Target.Transform.Position.WithZ( 64f ) - Transform.Rotation.Forward * Distance;
	}
}
