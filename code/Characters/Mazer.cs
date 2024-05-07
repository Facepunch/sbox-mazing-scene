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
	public TestMaze? Maze { get; set; }

	private Vector3 _targetDirection = new ( 1f, 0f, 0f );

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
		CharacterController.ApplyFriction( 4f );

		var input = MoveInput.LengthSquared > 1f ? MoveInput.Normal : MoveInput;

		UpdateTargetDirection( ref input );

		CharacterController.Accelerate( input * MoveSpeed );

		AnimationHelper.IsGrounded = true;
		AnimationHelper.WithWishVelocity( input * MoveSpeed );
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		var curRot = Transform.Rotation;
		var targetRot = Rotation.LookAt( _targetDirection, Vector3.Up );

		Transform.Rotation = Rotation.Slerp( curRot, targetRot, Helpers.Ease( 0.125f ) );

		CharacterController.Move();
	}

	private void UpdateTargetDirection( ref Vector2 input )
	{
		if ( input.LengthSquared < 0.01f )
		{
			input = Vector2.Zero;
			return;
		}

		_targetDirection = new Vector3( input.Normal, 0f );

		if ( Maze?.View is not {} view )
		{
			return;
		}

		var mazePos = Maze.WorldToMazePos( Transform.Position );
		var (row, col) = ((int) MathF.Floor( mazePos.x ), (int) MathF.Floor( mazePos.y ));

		var cellPos = mazePos - new Vector2( row, col );
		var dir = input.Normal;

		var north = view[row, col, Direction.North] == WallState.Open ? GetDist( cellPos, dir, new Vector2( 0f, 0f ), new Vector2( 1f, 0f ) ) : float.PositiveInfinity;
		var south = view[row, col, Direction.South] == WallState.Open ? GetDist( cellPos, dir, new Vector2( 1f, 0f ), new Vector2( -1f, 0f ) ) : float.PositiveInfinity;
		var west = view[row, col, Direction.West] == WallState.Open ? GetDist( cellPos, dir, new Vector2( 0f, 0f ), new Vector2( 0f, 1f ) ) : float.PositiveInfinity;
		var east = view[row, col, Direction.East] == WallState.Open ? GetDist( cellPos, dir, new Vector2( 0f, 1f ), new Vector2( 0f, -1f ) ) : float.PositiveInfinity;

		Direction forward;

		if ( north < east && north < west )
		{
			forward = Direction.North;
		}
		else if ( south < east && south < west )
		{
			forward = Direction.South;
		}
		else if ( west < east )
		{
			forward = Direction.West;
		}
		else if ( east > west )
		{
			forward = Direction.East;
		}
		else
		{
			return;
		}

		var right = forward.Clockwise();

		var forwardTarget = view[row, col, forward] == WallState.Open ? 1f : Vector2.Dot( cellPos - 0.5f, forward.GetNormal() ) * -2f;
		var rightTarget = Vector2.Dot( cellPos - 0.5f, right.GetNormal() ) * -2f;

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

	}
}
