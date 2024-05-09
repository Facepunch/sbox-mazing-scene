
using Sandbox.Citizen;
using System;

namespace Mazing;

public sealed class Holder : Component, Component.ITriggerListener, IThrowableListener
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	[Property]
	public Holdable? HeldItem { get; set; }

	[Property]
	public GameObject? HoldParent { get; set; }

	private Vector3 _heldVelocity;

	private GameObject? _leftHandIk;
	private GameObject? _rightHandIk;

	void IThrowableListener.Thrown( Direction dir, int range )
	{
		Throw( dir, range == 0 ? 0 : range + 1 );
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

	protected override void OnEnabled()
	{
		if ( HeldItem is not null )
		{
			return;
		}

		if ( Components.Get<Collider>() is { } collider )
		{
			foreach ( var touching in collider.Touching )
			{
				if ( touching.Components.Get<Holdable>() is { } holdable && TryPickUp( holdable ) )
				{
					break;
				}
			}
		}
	}

	protected override void OnDisabled()
	{
		Drop();
	}

	public void Drop()
	{
		Throw( Direction.North, 0 );
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void Throw( Direction dir, int range )
	{
		if ( HeldItem is null )
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

		var item = HeldItem;

		HeldItem.Holder = null;
		HeldItem = null;

		item.DispatchDropped();

		item.Throwable.Throw( dir, range );
	}

	public bool TryPickUp( Holdable item )
	{
		if ( IsProxy )
		{
			return false;
		}

		if ( !Enabled || HeldItem == item || item.Holder is not null )
		{
			return false;
		}

		if ( HeldItem is null )
		{
			PickUp( item.GameObject.Id );
			return true;
		}

		if ( HeldItem.Components.Get<Holder>() is { } heldHolder )
		{
			return heldHolder.TryPickUp( item );
		}

		return false;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void PickUp( Guid itemId )
	{
		var item = Scene.Directory.FindByGuid( itemId )
			?.Components.Get<Holdable>();

		HeldItem = item ?? throw new Exception( "Can't find item" );
		HeldItem.Holder = this;

		_heldVelocity = 0f;

		item.DispatchPickedUp();

		if ( Components.Get<CitizenAnimationHelper>() is { } animHelper )
		{
			_leftHandIk = new GameObject { Name = "Left Hand Target", Parent = GameObject };
			_rightHandIk = new GameObject { Name = "Right Hand Target", Parent = GameObject };

			animHelper.IkLeftHand = _leftHandIk;
			animHelper.IkRightHand = _rightHandIk;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( HeldItem is null )
		{
			return;
		}

		var holdTransform = (HoldParent ?? GameObject).Transform;
		var currentPos = HeldItem.Transform.Position;
		var targetPos = holdTransform.Position;

		var accel = (targetPos - currentPos) * 150f;

		if ( Components.Get<CharacterController>() is { } charController )
		{
			accel += charController.Velocity * 20f;
		}

		_heldVelocity = Vector3.Lerp( _heldVelocity, 0f, Helpers.Ease( 0.2f ) );
		_heldVelocity += accel * Time.Delta;

		HeldItem.Transform.Position += _heldVelocity * Time.Delta;

		if ( _leftHandIk.IsValid() && _rightHandIk.IsValid() )
		{
			var pos = HeldItem.Transform.Position + Transform.Rotation.Up * 16f;
			var right = Transform.Rotation.Right * 12f;

			_leftHandIk.Transform.Position = pos - right;
			_rightHandIk.Transform.Position = pos + right;

			var forwardAmount = Math.Clamp( Vector3.Dot( Transform.Rotation.Forward, currentPos - targetPos ) / 16f, -0.75f, 0.75f );

			_leftHandIk.Transform.LocalRotation = Rotation.From( -45f, 90f, 0f + MathF.Asin( forwardAmount ).RadianToDegree() );
			_rightHandIk.Transform.LocalRotation = Rotation.From( -45f, -90f, 180f - MathF.Asin( forwardAmount ).RadianToDegree() );
		}
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.Components.Get<Holdable>() is not { } holdable )
		{
			return;
		}

		if ( !holdable.PickupOnTouch || holdable.Throwable.Enabled || holdable.Holder is not null )
		{
			return;
		}

		TryPickUp( holdable );
	}
}
