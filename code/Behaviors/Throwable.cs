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

	[Property]
	public int IndexOnFloor { get; set; }

	[Property, Sync]
	public bool IsExiting { get; set; }

	[Property, Sync] public bool CanExit { get; set; } = true;

	public Vector3 Velocity { get; private set; }

	public delegate void ThrownAction( Direction direction, int range );

	[Property] public event ThrownAction? Thrown;
	[Property] public event Action? Landed;

	private Vector3 _throwStart;
	private Vector3 _throwEnd;
	private TimeUntil _throwEndTime;
	private float _throwHeight;

	protected override void OnAwake()
	{
		Enabled = false;
	}

	[Broadcast]
	public void Throw( int fromRow, int fromCol, Direction dir, int range )
	{
		var increment = dir.GetNeighbor( 0, 0 );
		var actualRange = 0;

		while ( actualRange < range )
		{
			if ( MazeObject.View[fromRow + increment.Row, fromCol + increment.Col] == CellState.Empty )
			{
				break;
			}

			fromRow += increment.Row;
			fromCol += increment.Col;

			++actualRange;
		}

		ThrowInternal( fromRow, fromCol );

		if ( IsProxy ) return;

		Thrown?.Invoke( dir, actualRange );

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Thrown( dir, actualRange );
		}
	}

	private void ThrowInternal( int row, int col )
	{
		Enabled = true;
		IndexOnFloor = 0;

		_throwStart = Transform.Position;

		_throwEnd = MazeObject.Maze.MazeToWorldPos( row, col );

		var dist = Math.Max( 0.5f, (_throwEnd - _throwStart).WithZ( 0f ).Length / 48f );
		var sqrtDist = MathF.Sqrt( dist );

		_throwEndTime = sqrtDist * 0.5f;
		_throwHeight = sqrtDist * 64f;

		Velocity = default;
	}

	protected override void OnUpdate()
	{
		if ( IsExiting )
		{
			if ( Transform.Position.z > -1024f )
			{
				Transform.Position -= new Vector3( 0f, 0f, 400f ) * Time.Delta;
			}

			return;
		}

		var duration = _throwEndTime.Passed + _throwEndTime;

		var x = Math.Clamp( _throwEndTime.Fraction, 0f, 1f );
		var y = -4f * x * x + 4f * x;

		var up = Vector3.Up * _throwHeight;
		var along = _throwEnd - _throwStart;

		Transform.Position = _throwStart + along * x + up * y;

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

		Enabled = false;

		var cellIndex = MazeObject.CellIndex;

		Landed?.Invoke();

		foreach ( var listener in Components.GetAll<IThrowableListener>( FindMode.Enabled | FindMode.Disabled | FindMode.InSelf ) )
		{
			listener.Landed();
		}

		var landedOn = Scene.Components
			.GetAll<Throwable>( FindMode.InChildren | FindMode.Disabled )
			.Where( other => other.GameObject != GameObject )
			.Where( other => other.GameObject.Enabled && !other.Enabled )
			.Where( other => other.MazeObject.CellIndex == cellIndex )
			.MaxBy( other => other.IndexOnFloor );

		if ( landedOn != null )
		{
			if ( Components.Get<Holdable>() is {} holdable
				&& landedOn.Components.Get<Holder>() is { } holder
				&& holder.TryPickUp( holdable ) )
			{
				return;
			}

			IndexOnFloor = landedOn.IndexOnFloor + 1;
			Transform.Position = Transform.Position.WithZ( IndexOnFloor * 16f );
		}
		else
		{
			IndexOnFloor = 0;
			Transform.Position = Transform.Position.WithZ( 0f );
		}
	}
}
