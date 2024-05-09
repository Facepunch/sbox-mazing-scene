using System;
using Sandbox;
using Sandbox.Citizen;

namespace Mazing;

public enum MazerState
{
	Falling,
	Walking,
	Vaulting
}

public sealed class Mazer : Component
{
	[RequireComponent] public Throwable Throwable { get; set; } = null!;
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;
	[RequireComponent] public Collider Trigger { get; set; } = null!;
	[RequireComponent] public CharacterController CharacterController { get; set; } = null!;
	[RequireComponent] public CitizenAnimationHelper AnimationHelper { get; set; } = null!;

	[Property, Sync]
	public Vector2 MoveInput { get; set; }

	[Property, Sync]
	public MazerState State { get; set; }

	[Property, Sync]
	public float MoveSpeed { get; set; } = 120f;

	[Property, Sync]
	public float VaultCooldown { get; set; } = 3f;

	[Property, Sync]
	public TimeUntil NextVault { get; set; }

	public bool CanVault => NextVault > 0f;

	public Direction Direction => _targetLook.GetDirection();

	[Property] public event Action? Vaulted;
	[Property] public event Action<int>? VaultCooldownTick;
	[Property] public event Action? VaultReady;
	[Property] public event Action<SceneModel.FootstepEvent> Footstep;

	private Vector2 _targetLook = Direction.West.GetNormal();

	private int _lastCooldownTick;

	protected override void OnStart()
	{
		AnimationHelper.Target.OnFootstepEvent += Footstep;
	}

	public bool TryVault()
	{
		if ( State != MazerState.Walking || NextVault > 0f )
		{
			return false;
		}

		var dir = _targetLook.GetDirection();
		var (row, col) = MazeObject.CellIndex;

		if ( MazeObject.View[row, col, dir] == WallState.Open )
		{
			return false;
		}

		var target = dir.GetNeighbor( row, col );

		if ( MazeObject.View[target.Row, target.Col] == CellState.Empty )
		{
			return false;
		}

		NextVault = VaultCooldown;

		Throwable.Throw( dir, 1 );

		Vaulted?.Invoke();

		return true;
	}

	protected override void OnUpdate()
	{
		var cooldownTick = Math.Max( 0, (int)MathF.Ceiling( NextVault ) );

		if ( _lastCooldownTick != cooldownTick )
		{
			_lastCooldownTick = cooldownTick;

			VaultCooldownTick?.Invoke( cooldownTick );

			if ( cooldownTick == 0 )
			{
				VaultReady?.Invoke();
			}
		}

		if ( State != MazerState.Vaulting && Throwable.Enabled )
		{
			State = MazerState.Vaulting;

			AnimationHelper.TriggerJump();
		}

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
		CharacterController.ApplyFriction( 4f );

		var input = MoveInput.LengthSquared > 1f ? MoveInput.Normal : MoveInput;

		if ( input.LengthSquared <= 0.01f )
		{
			input = Vector2.Zero;
		}
		else
		{
			_targetLook = input.Normal;

			AlignMovementToGrid( ref input );
		}

		CharacterController.Accelerate( input * MoveSpeed );

		AnimationHelper.IsGrounded = true;
		AnimationHelper.WithWishVelocity( input * MoveSpeed );
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		var curRot = Transform.Rotation;
		var targetRot = Rotation.LookAt( _targetLook, Vector3.Up );

		Transform.Rotation = Rotation.Slerp( curRot, targetRot, Helpers.Ease( 0.125f ) );

		CharacterController.Move();

		if ( IsProxy )
		{
			return;
		}

		if ( !CharacterController.IsOnGround )
		{
			var (row, col) = MazeObject.CellIndex;
			Throwable.Throw( row, col );
		}
	}

	private void AlignMovementToGrid( ref Vector2 input )
	{
		var (row, col) = MazeObject.CellIndex;
		var cellPos = MazeObject.CellLocalPos;
		var dir = input.Normal;

		Direction? bestDir = null;
		var minDist = float.PositiveInfinity;

		foreach ( var direction in Helpers.Directions )
		{
			var norm = direction.GetNormal();
			var dist = GetDist( cellPos, dir, new Vector2( 0.5f, 0.5f ) + norm * 0.5f, -norm );

			if ( dist < 0f )
			{
				continue;
			}

			if ( MazeObject.View[row, col, direction] != WallState.Open )
			{
				dist += 2f;
			}

			if ( dist < minDist )
			{
				bestDir = direction;
				minDist = dist;
			}
		}

		if ( bestDir is not { } forward )
		{
			return;
		}

		var right = forward.Clockwise();

		var forwardTarget = MazeObject.View[row, col, forward] == WallState.Open ? 1f : Vector2.Dot( cellPos - 0.5f, forward.GetNormal() ) * -8f;
		var rightTargetVel = Vector2.Dot( cellPos - 0.5f, right.GetNormal() ) * -8f;
		var rightVel = CharacterController.Velocity.Dot( right.GetNormal() ) / MoveSpeed;
		var rightTarget = rightTargetVel - rightVel;

		forwardTarget = Math.Clamp( forwardTarget, -1f, 1f );
		rightTarget = Math.Clamp( rightTarget, -1f, 1f );

		var targetInput = forward.GetNormal() * forwardTarget + right.GetNormal() * rightTarget;

		if ( targetInput.LengthSquared > 1f )
		{
			targetInput = targetInput.Normal;
		}

		input = targetInput * input.Length;
	}

	private float GetDist( Vector2 pos, Vector2 dir, Vector2 origin, Vector2 normal )
	{
		var denom = Vector2.Dot( dir, normal );

		if ( denom >= -0.00001f )
		{
			return float.PositiveInfinity;
		}

		return Vector2.Dot( origin - pos, normal ) / denom;
	}

	private void OnFalling()
	{
		if ( Transform.Position.z < -1024f )
		{
			return;
		}

		AnimationHelper.IsGrounded = false;
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		CharacterController.Velocity = CharacterController.Velocity.WithX( 0f ).WithY( 0f );
		CharacterController.Accelerate( Vector3.Up * -600f );
		CharacterController.Move();

		if ( IsProxy )
		{
			return;
		}

		if ( CharacterController.IsOnGround )
		{
			State = MazerState.Walking;
		}
	}

	private void OnVaulting()
	{
		CharacterController.Enabled = false;
		CharacterController.Velocity = Vector3.Zero;

		AnimationHelper.IsGrounded = false;
		AnimationHelper.WithVelocity( Throwable.Velocity );

		if ( IsProxy )
		{
			return;
		}

		if ( Throwable.Active )
		{
			CharacterController.Enabled = true;
			CharacterController.IsOnGround = false;

			State = MazerState.Falling;
		}
	}
}
