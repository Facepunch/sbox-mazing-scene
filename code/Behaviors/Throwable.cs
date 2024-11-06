using System;

namespace Mazing;

public interface IThrowableListener
{
	public void Thrown( int fromRow, int fromCol, Direction dir, int range ) { }
	public void Landed() { }
}

public sealed class Throwable : Component
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	[Property]
	public float ThrowHeight { get; set; } = 64f;

	[Property]
	public float ThrowDuration { get; set; } = 0.5f;

	[Property, Sync]
	public int IndexOnFloor { get; set; }

	[Property, Sync]
	public bool IsExiting { get; set; }

	[Property, Sync]
	public bool IsAirborne { get; set; }

	[Property, Sync] public bool CanExit { get; set; } = true;

	public Vector3 Velocity { get; private set; }

	public delegate void ThrownAction( Direction direction, int range );

	[Property] public event ThrownAction? Thrown;
	[Property] public event Action? Landed;

	private Vector3 _throwStart;
	private Vector3 _throwEnd;
	private TimeUntil _throwEndTime;
	private float _throwHeight;

	[Broadcast]
	public void Throw( int fromRow, int fromCol, Direction dir, int range )
	{
		var increment = dir.GetNeighbor( 0, 0 );
		var actualRange = 0;

		var (row, col) = (fromRow, fromCol);

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

		ThrowInternal( row, col, range );

		if ( IsProxy ) return;

		Thrown?.Invoke( dir, actualRange );

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Thrown( fromRow, fromCol, dir, range );
		}
	}

	private void ThrowInternal( int row, int col, int range )
	{
		GameObject.SetParent( null );

		IsAirborne = true;
		IndexOnFloor = 0;

		_throwStart = WorldPosition;
		_throwEnd = MazeObject.Maze.MazeToWorldPos( row, col );

		var dist = Math.Max( 0.5f, range );
		var sqrtDist = MathF.Sqrt( dist );

		_throwEndTime = sqrtDist * 0.5f;
		_throwHeight = sqrtDist * 64f;

		Velocity = default;
	}

	protected override void OnUpdate()
	{
		if ( !IsAirborne )
		{
			return;
		}

		if ( IsExiting )
		{
			if ( WorldPosition.z > -1024f )
			{
				WorldPosition -= new Vector3( 0f, 0f, 400f ) * Time.Delta;
			}

			return;
		}

		var duration = _throwEndTime.Passed + _throwEndTime;

		var x = Math.Clamp( _throwEndTime.Fraction, 0f, 1f );
		var y = -4f * x * x + 4f * x;

		var up = Vector3.Up * _throwHeight;
		var along = _throwEnd - _throwStart;

		WorldPosition = _throwStart + along * x + up * y;

		var dt = 1f / duration;
		var dydx = -8f * x + 4f;

		Velocity = along * dt + up * dydx * dt;

		if ( IsProxy ) return;
		if ( x < 1f ) return;

		if ( MazeObject.GetObjectsInSameCell().Any( x => x.Components.Get<Exit>() is { IsOpen: true } ) )
		{
			IsExiting = true;
			return;
		}

		var landedOn = MazeObject.GetObjectsInSameCell()
			.Select( obj => obj.Components.Get<Throwable>( true ) )
			.FirstOrDefault( obj => obj != null );

		IsAirborne = false;

		Landed?.Invoke();

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Landed();
		}

		if ( landedOn is not null )
		{
			if ( Components.Get<Holdable>() is {} holdable
				&& landedOn.Components.Get<Holder>() is { } holder
				&& holder.TryPickUp( holdable, true ) )
			{
				return;
			}

			IndexOnFloor = landedOn.IndexOnFloor + 1;
			WorldPosition = WorldPosition.WithZ( IndexOnFloor * 16f );
		}
		else
		{
			IndexOnFloor = 0;
			WorldPosition = WorldPosition.WithZ( 0f );
		}
	}
}
