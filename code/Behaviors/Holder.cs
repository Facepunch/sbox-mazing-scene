using Sandbox.Citizen;
using System;
using System.Text;

namespace Mazing;

public sealed class Holder : Component, IThrowableListener
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	private bool _heldItemsInvalid = true;
	private readonly List<Holdable> _heldItems = new();

	public IReadOnlyList<Holdable> HeldItems
	{
		get
		{
			if ( _heldItemsInvalid )
			{
				_heldItemsInvalid = false;

				_heldItems.Clear();
				_heldItems.AddRange( GameObject.Children
					.Select( x => x.Components.Get<Holdable>() )
					.Where( x => x.IsValid() ) );
			}
			else
			{
				_heldItems.RemoveAll( x => !x.IsValid() );
			}

			return _heldItems;
		}
	}

	[Property] public bool AutoThrow { get; set; } = true;

	[Property, HideIf( nameof(AutoThrow), false )]
	public float AutoThrowTime { get; set; } = 0.5f;

	private GameObject? _leftHandIk;
	private GameObject? _rightHandIk;

	void IThrowableListener.Thrown( int fromRow, int fromCol, Direction dir, int range )
	{
		ThrowAll( fromRow, fromCol, dir, range == 0 ? 0 : range + 1 );
		Enabled = false;
	}

	void IThrowableListener.Landed()
	{
		Enabled = true;
	}

	protected override void OnDestroy()
	{
		Drop();

		_rightHandIk?.Destroy();
		_rightHandIk = null;

		_leftHandIk?.Destroy();
		_leftHandIk = null;
	}

	protected override void OnDisabled()
	{
		Drop();
	}

	public void Drop()
	{
		ThrowAll( Direction.North, 0 );
	}

	public void ThrowAll( Direction dir, int range )
	{
		var (row, col) = MazeObject.CellIndex;
		ThrowAll( row, col, dir, range );
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void ThrowAll( int fromRow, int fromCol, Direction dir, int range )
	{
		var items = HeldItems;
		var count = items.Count;

		for ( var i = 0; i < count && items.Count > 0; ++i)
		{
			ThrowOneInternal( items[0], fromRow, fromCol, dir, range == 0 ? 0 : range + i );
		}
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void ThrowOne( Direction dir, int range )
	{
		if ( HeldItems is not { Count: > 0 } items ) return;

		var (row, col) = MazeObject.CellIndex;
		ThrowOneInternal( items[0], row, col, dir, range );
	}

	private void ThrowOneInternal( Holdable item, int fromRow, int fromCol, Direction dir, int range )
	{
		if ( HeldItems is not { Count: > 0 } items )
		{
			return;
		}

		if ( !items.Contains( item ) || item.Holder != this )
		{
			return;
		}

		var isFirst = items.First() == item;

		item.GameObject.SetParent( null );
		item.Network.ClearInterpolation();

		item.DispatchDropped();

		item.Throwable.Throw( fromRow, fromCol, dir, range );

		_heldItemsInvalid = true;

		if ( isFirst && HeldItems.Count > 0 )
		{
			HeldItems[0].HeldTime = 0f;
			return;
		}

		StopHolding();
	}

	private void StopHolding()
	{
		if ( _leftHandIk is null && _rightHandIk is null )
		{
			return;
		}

		_rightHandIk?.Destroy();
		_rightHandIk = null;

		_leftHandIk?.Destroy();
		_leftHandIk = null;

		if ( Components.Get<CitizenAnimationHelper>() is { } animHelper )
		{
			animHelper.IkLeftHand = null;
			animHelper.IkRightHand = null;
		}
	}

	public bool TryPickUp( Holdable item, bool airborne )
	{
		if ( IsProxy && item.IsProxy )
		{
			return false;
		}

		if ( !Enabled || item.Holder is not null )
		{
			return false;
		}

		if ( HeldItems.Count > 0 && HeldItems[0].Components.Get<Holder>() is { } heldholder )
		{
			return heldholder.TryPickUp( item, airborne );
		}

		PickUp( item, airborne );
		return true;
	}

	[Broadcast]
	public void PickUp( Holdable item, bool airborne )
	{
		_heldItemsInvalid = true;

		if ( !airborne && HeldItems.FirstOrDefault() is {} first )
		{
			first.GameObject.AddSibling( item.GameObject, true );
		}
		else
		{
			item.GameObject.SetParent( GameObject );
			item.Network.ClearInterpolation();
		}

		item.HeldVelocity = 0f;
		item.HeldTime = 0f;
		item.DispatchPickedUp();

		if ( _leftHandIk is not null )
		{
			return;
		}

		if ( Components.Get<CitizenAnimationHelper>() is not { } animHelper )
		{
			return;
		}

		_leftHandIk = new GameObject { Name = "Left Hand Target", Parent = GameObject };
		_rightHandIk = new GameObject { Name = "Right Hand Target", Parent = GameObject };

		animHelper.IkLeftHand = _leftHandIk;
		animHelper.IkRightHand = _rightHandIk;
	}

	protected override void OnUpdate()
	{
		UpdateHeldItemPositions();

		if ( IsProxy )
		{
			return;
		}

		if ( HeldItems is { Count: > 0 } items )
		{
			var (row, col) = MazeObject.CellIndex;

			for ( var i = items.Count - 1; i >= 0; --i )
			{
				if ( !items[i].Enabled )
				{
					ThrowOneInternal( items[i], row, col, Direction.North, 0 );
				}
			}
		}

		foreach ( var mazeObject in MazeObject.GetObjectsInSameCell() )
		{
			if ( mazeObject.Components.Get<Holdable>() is not {} holdable )
			{
				continue;
			}

			if ( mazeObject.Components.Get<Holder>() is not null )
			{
				continue;
			}

			TryPickUp( holdable, false );
		}

		if ( AutoThrow && HeldItems.Count > 0 && HeldItems[0].HeldTime > AutoThrowTime )
		{
			var dir = ((Vector2)Transform.Rotation.Forward).GetDirection();

			ThrowOne( (Direction)(((int)dir + Random.Shared.Next( 1, 4 )) % 4), 1 );
		}
	}

	private void UpdateHeldItemPositions()
	{
		_heldItemsInvalid = true;

		if ( HeldItems.Count == 0 )
		{
			StopHolding();
			return;
		}

		if ( _leftHandIk.IsValid() && _rightHandIk.IsValid() )
		{
			var pos = HeldItems[0].Transform.Position + Transform.Rotation.Up * 16f;
			var right = Transform.Rotation.Right * 12f;

			_leftHandIk.Transform.Position = pos - right;
			_rightHandIk.Transform.Position = pos + right;

			var forwardAmount = Math.Clamp( Vector3.Dot( Transform.Rotation.Forward, pos - Transform.Position ) / 16f, -0.75f, 0.75f );

			_leftHandIk.Transform.LocalRotation = Rotation.From( -45f, 90f, 0f + MathF.Asin( forwardAmount ).RadianToDegree() );
			_rightHandIk.Transform.LocalRotation = Rotation.From( -45f, -90f, 180f - MathF.Asin( forwardAmount ).RadianToDegree() );
		}
	}
}
