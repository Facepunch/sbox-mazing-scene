using System;

namespace Mazing;

public interface IThrowableListener
{
	public void Thrown( Direction dir, int range ) { }
	public void Landed() { }
}

public sealed class Throwable : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	[Property]
	public float ThrowHeight { get; set; } = 64f;

	[Property]
	public float ThrowDuration { get; set; } = 0.5f;

	public Vector3 Velocity { get; private set; }

	public delegate void ThrownAction( Direction direction, int range );

	[Property] public event ThrownAction? Thrown;
	[Property] public event Action? Landed;

	private Vector3 _throwStart;
	private Vector3 _throwEnd;
	private TimeUntil _throwEndTime;
	private float _throwHeight;

	protected override void OnStart()
	{
		Enabled = false;
	}

	[Broadcast]
	public void Throw( Direction dir, int range )
	{
		var (row, col) = MazeObject.CellIndex;

		var increment = dir.GetNeighbor( 0, 0 );
		var actualRange = 0;

		while ( actualRange < range )
		{
			if ( MazeObject.View[row + increment.Row, col + increment.Col] == CellState.Empty )
			{
				break;
			}

			row += increment.Row;
			col += increment.Col;

			++actualRange;
		}

		ThrowInternal( row, col );

		if ( IsProxy ) return;

		Thrown?.Invoke( dir, actualRange );

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Thrown( dir, actualRange );
		}
	}

	[Broadcast]
	public void Throw( int row, int col )
	{
		ThrowInternal( row, col );

		Thrown?.Invoke( Direction.North, 0 );
	}

	private void ThrowInternal( int row, int col )
	{
		Enabled = true;

		_throwStart = Transform.Position;

		_throwEnd = MazeObject.Maze.MazeToWorldPos( row, col );

		var dist = (_throwEnd - _throwStart).WithZ( 0f ).Length / 48f;
		var sqrtDist = MathF.Sqrt( dist );

		_throwEndTime = sqrtDist * 0.5f;
		_throwHeight = sqrtDist * 64f;

		Velocity = default;
	}

	protected override void OnUpdate()
	{
		var duration = _throwEndTime.Passed + _throwEndTime;

		var x = Math.Clamp( _throwEndTime.Fraction, 0f, 1f );
		var y = -4f * x * x + 4f * x;

		var up = Vector3.Up * _throwHeight;
		var along = _throwEnd - _throwStart;

		Transform.Position = _throwStart + along * x + up * y;

		var dt = 1f / duration;
		var dydx = -8f * x + 4f;

		Velocity = along * dt + up * dydx * dt;

		if ( x < 1f ) return;

		Enabled = false;

		if ( IsProxy ) return;

		Landed?.Invoke();

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Landed();
		}
	}
}
