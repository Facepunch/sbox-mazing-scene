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
		if ( Maze is null or { IsValid: false } )
		{
			Maze = Scene.Components.Get<TestMaze>( FindMode.Enabled | FindMode.InChildren );
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
			_targetDirection = new Vector3( input.Normal, 0f );

			AlignMovementToGrid( ref input );
		}

		CharacterController.Accelerate( input * MoveSpeed );

		AnimationHelper.IsGrounded = true;
		AnimationHelper.WithWishVelocity( input * MoveSpeed );
		AnimationHelper.WithVelocity( CharacterController.Velocity );

		var curRot = Transform.Rotation;
		var targetRot = Rotation.LookAt( _targetDirection, Vector3.Up );

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

		var mazePos = Maze.WorldToMazePos( Transform.Position );
		var (row, col) = ((int) MathF.Floor( mazePos.x ), (int) MathF.Floor( mazePos.y ));

		var cellPos = mazePos - new Vector2( row, col );
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

	}
}
