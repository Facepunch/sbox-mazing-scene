using Sandbox.Citizen;
using System;
using System.Text;

namespace Mazing;

public sealed class Holder : Component, IThrowableListener
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	public List<Holdable> HeldItems { get; } = new();

	[Property] public bool AutoThrow { get; set; } = true;

	[Property, HideIf( nameof(AutoThrow), false )]
	public float AutoThrowTime { get; set; } = 0.5f;

	private GameObject? _leftHandIk;
	private GameObject? _rightHandIk;

	void IThrowableListener.Thrown( Direction dir, int range )
	{
		ThrowAll( dir, range == 0 ? 0 : range + 1 );
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

	[Broadcast( NetPermission.OwnerOnly )]
	public void ThrowAll( Direction dir, int range )
	{
		var count = HeldItems.Count;

		for ( var i = 0; i < count; ++i)
		{
			ThrowOne( dir, range == 0 ? 0 : range + i );
		}
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void ThrowOne( Direction dir, int range )
	{
		ThrowOneInternal( dir, range );
	}

	private void ThrowOneInternal( Direction dir, int range )
	{
		if ( HeldItems.Count == 0 )
		{
			return;
		}

		var item = HeldItems[0];
		HeldItems.RemoveAt( 0 );

		item.GameObject.SetParent( null );
		item.Holder = null;
		item.DispatchDropped();

		var (row, col) = MazeObject.CellIndex;

		item.Throwable.Throw( row, col, dir, range );

		if ( HeldItems.Count > 0 )
		{
			HeldItems[0].HeldTime = 0f;
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

		PickUp( item.GameObject.Id, airborne );
		return true;
	}

	[Broadcast]
	public void PickUp( Guid itemId, bool airborne )
	{
		var item = Scene.Directory.FindByGuid( itemId )
			?.Components.Get<Holdable>()
			?? throw new Exception( "Can't find item" );

		item.Holder = this;

		if ( airborne )
		{
			HeldItems.Add( item );
		}
		else
		{
			HeldItems.Insert( 0, item );
		}

		item.GameObject.SetParent( GameObject );
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
		if ( HeldItems.Count == 0 )
		{
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
