
using System;
using static Sandbox.PhysicsContact;

namespace Mazing;

public sealed class MeleeAttacker : Component
{
	[Property]
	public float Range { get; set; } = 32f;

	[Property]
	public float ResetTime { get; set; } = 1f;

	[Property]
	public TimeUntil NextAttack { get; set; }

	public delegate void AttackAction( Player target );

	[Property]
	public event AttackAction? Attacked;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( NextAttack > 0f )
		{
			return;
		}

		var pos = (Vector2) Transform.Position;

		var target = Scene.Components.GetAll<Player>( FindMode.Enabled | FindMode.InChildren )
			.Where( x => x.Mazer.State == MazerState.Walking )
			.Where( x => ((Vector2)x.Transform.Position - pos).LengthSquared <= Range * Range )
			.MinBy( x => ((Vector2)x.Transform.Position - pos).LengthSquared );

		if ( target is null )
		{
			return;
		}

		NextAttack = 0f;

		target.Mazer.Kill();

		DispatchAttacked( target.Id );
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void DispatchAttacked( Guid targetGuid )
	{
		var target = Scene.Directory.FindComponentByGuid( targetGuid );

		if ( target is Player player )
		{
			Attacked?.Invoke( player );
		}
	}
}
