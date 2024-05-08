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
	[Property]
	public Vector2 MoveInput { get; set; }

	[Property]
	public MazerState State { get; set; }

	[Property]
	public float MoveSpeed { get; set; } = 120f;

	[Property]
	public float VaultDuration { get; set; } = 0.5f;

	[Property]
	public float VaultCooldown { get; set; } = 3f;

	[Property]
	public float VaultHeight { get; set; } = 64f;

	public TimeUntil NextVault { get; set; }

	public bool CanVault => NextVault > 0f;

	[Property]
	public TestMaze? Maze { get; set; }

	[Property] public event Action? Vaulted;
	[Property] public event Action<int>? VaultCooldownTick;
	[Property] public event Action? VaultReady;

	public Vector2 MazeLocalPos => Maze?.WorldToMazePos( Transform.Position ) ?? default;

	public Vector2 CellLocalPos
	{
		get
		{
			var mazeLocalPos = MazeLocalPos;
			return mazeLocalPos - new Vector2( MathF.Floor( mazeLocalPos.x ), MathF.Floor( mazeLocalPos.y ) );
		}
	}

	public (int Row, int Col) CellIndex
	{
		get
		{
			if ( Maze is null )
			{
				return (0, 0);
			}

			var mazePos = Maze.WorldToMazePos( Transform.Position );
			return ((int)MathF.Floor( mazePos.x ), (int)MathF.Floor( mazePos.y ));
		}
	}

	private Vector2 _targetLook = Direction.West.GetNormal();

	private Vector3 _vaultStart;
	private Vector3 _vaultEnd;
	private TimeUntil _vaultEndTime;
	private float _vaultHeight;
	private int _lastCooldownTick;

	[RequireComponent] public CharacterController CharacterController { get; set; } = null!;
	[RequireComponent] public CitizenAnimationHelper AnimationHelper { get; set; } = null!;

	public bool TryVault()
	{
		if ( Maze is null )
		{
			return false;
		}

		if ( State != MazerState.Walking || NextVault > 0f )
		{
			return false;
		}

		var dir = _targetLook.GetDirection();
		var (row, col) = CellIndex;

		if ( Maze.View![row, col, dir] == WallState.Open )
		{
			return false;
		}

		var target = dir.GetNeighbor( row, col );

		if ( Maze.View[target.Row, target.Col] == CellState.Empty )
		{
			return false;
		}

		NextVault = VaultCooldown;

		VaultTo( target.Row, target.Col );

		Vaulted?.Invoke();

		return true;
	}

	public void VaultTo( int row, int col )
	{
		if ( Maze is null )
		{
			return;
		}

		_vaultStart = Transform.Position;

		_vaultEnd = Maze.MazeToWorldPos( row, col );

		var dist = (_vaultEnd - _vaultStart).WithZ( 0f ).Length / 48f;
		var sqrtDist = MathF.Sqrt( dist );

		_vaultEndTime = sqrtDist * VaultDuration;
		_vaultHeight = sqrtDist * VaultHeight;

		State = MazerState.Vaulting;

		CharacterController.Enabled = false;

		AnimationHelper.IsGrounded = false;
		AnimationHelper.TriggerJump();
	}

	protected override void OnUpdate()
	{
		if ( Maze is null or { IsValid: false } )
		{
			Maze = Scene.Components.Get<TestMaze>( FindMode.Enabled | FindMode.InChildren );
		}

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
	}

	private static Direction[] Directions { get; } = new[]
	{
		Direction.West, Direction.North, Direction.East, Direction.South
	};

	private void AlignMovementToGrid( ref Vector2 input )
	{
		if ( Maze?.View is not {} view )
		{
			return;
		}

		var (row, col) = CellIndex;
		var cellPos = CellLocalPos;
		var dir = input.Normal;

		Direction? bestDir = null;
		var minDist = float.PositiveInfinity;

		foreach ( var direction in Directions )
		{
			var norm = direction.GetNormal();
			var dist = GetDist( cellPos, dir, new Vector2( 0.5f, 0.5f ) - norm * 0.5f, -norm );

			if ( view[row, col, direction] != WallState.Open )
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

		var forwardTarget = view[row, col, forward] == WallState.Open ? 1f : Vector2.Dot( cellPos - 0.5f, forward.GetNormal() ) * -2f;
		var rightTargetVel = Vector2.Dot( cellPos - 0.5f, right.GetNormal() ) * -2f;
		var rightVel = CharacterController.Velocity.Dot( right.GetNormal() ) / MoveSpeed;

		var targetInput = forward.GetNormal() * forwardTarget + right.GetNormal() * (rightTargetVel - rightVel);

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

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( Maze is null )
		{
			return;
		}

		using var scope = Gizmo.Scope();

		Gizmo.Transform = global::Transform.Zero;

		var mazePos = Maze.WorldToMazePos( Transform.Position );
		var cellPos = (Row: (int) MathF.Floor( mazePos.x ), Col: (int) MathF.Floor( mazePos.y ));

		var cellRect = Maze.GetCellWorldRect( cellPos.Row, cellPos.Col );

		Gizmo.Draw.LineBBox( new BBox( cellRect.Position, cellRect.Position + cellRect.Size ) );
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
		var duration = _vaultEndTime.Passed + _vaultEndTime;

		var x = Math.Clamp( _vaultEndTime.Fraction, 0f, 1f );
		var y = -4f * x * x + 4f * x;

		var up = Vector3.Up * _vaultHeight;
		var along = _vaultEnd - _vaultStart;

		Transform.Position = _vaultStart + along * x + up * y;

		CharacterController.Velocity = Vector3.Zero;

		var dt = 1f / duration;
		var dydx = -8f * x + 4f;

		AnimationHelper.IsGrounded = false;
		AnimationHelper.WithVelocity( along * dt + up * dydx * dt );

		if ( x >= 1f )
		{
			CharacterController.Enabled = true;
			CharacterController.IsOnGround = false;

			State = MazerState.Falling;
		}
	}
}
