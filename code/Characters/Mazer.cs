using Sandbox;
using Sandbox.Citizen;

public enum MazerState
{
	Falling,
	Walking,
	Vaulting
}

public sealed class Mazer : Component
{
	[Property]
	public Vector2 MoveInput { get; set; }

	[Property]
	public MazerState State { get; set; }

	[Property]
	public float MoveSpeed { get; set; } = 120f;

	[RequireComponent] public CharacterController CharacterController { get; set; } = null!;
	[RequireComponent] public CitizenAnimationHelper AnimationHelper { get; set; } = null!;

	protected override void OnUpdate()
	{
		switch ( State )
		{
			case MazerState.Walking:
				OnWalking();
				return;

			case MazerState.Falling:
				OnFalling();
				return;

			case MazerState.Vaulting:
				OnVaulting();
				return;
		}
	}

	private void OnWalking()
	{
		var input = MoveInput.LengthSquared > 1f ? MoveInput.Normal : MoveInput;

		AnimationHelper.IsGrounded = true;
		AnimationHelper.WithWishVelocity( input * MoveSpeed );
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		CharacterController.ApplyFriction( 4f );
		CharacterController.Accelerate( input * MoveSpeed );
		CharacterController.Move();
	}

	private void OnFalling()
	{
		AnimationHelper.IsGrounded = false;
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		CharacterController.Velocity = CharacterController.Velocity.WithX( 0f ).WithY( 0f );
		CharacterController.Accelerate( Vector3.Up * -600f );
		CharacterController.Move();

		if ( CharacterController.IsOnGround )
		{
			State = MazerState.Walking;
		}
	}

	private void OnVaulting()
	{

	}
}
