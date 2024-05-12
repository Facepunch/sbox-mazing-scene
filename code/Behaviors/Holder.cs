using Sandbox.Citizen;
using System;

namespace Mazing;

public sealed class Holder : Component, IThrowableListener
{
	[RequireComponent] public MazeObject MazeObject { get; set; } = null!;

	public List<Holdable> HeldItems { get; } = new();

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
		if ( HeldItems.Count == 0 )
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

		for ( var i = 0; i < HeldItems.Count; i++ )
		{
			var item = HeldItems[i];

			item.Holder = null;
			item.DispatchDropped();

			var (row, col) = MazeObject.CellIndex;

			item.Throwable.Throw( row, col, dir, range == 0 ? 0 : range + i );
		}

		HeldItems.Clear();
	}

	public bool TryPickUp( Holdable item )
	{
		if ( IsProxy )
		{
			return false;
		}

		if ( !Enabled || item.Holder is not null )
		{
			return false;
		}

		if ( HeldItems.Count > 0 && HeldItems[0].Components.Get<Holder>() is { } heldholder )
		{
			return heldholder.TryPickUp( item );
		}

		PickUp( item.GameObject.Id );
		return true;
	}

	public void PickUp( Guid itemId )
	{
		var item = Scene.Directory.FindByGuid( itemId )
			?.Components.Get<Holdable>()
			?? throw new Exception( "Can't find item" );

		item.Holder = this;

		HeldItems.Insert( 0, item );

		item.HeldVelocity = 0f;
		item.DispatchPickedUp();

		if ( Components.Get<CitizenAnimationHelper>() is { } animHelper )
		{
			_leftHandIk = new GameObject { Name = "Left Hand Target", Parent = GameObject };
			_rightHandIk = new GameObject { Name = "Right Hand Target", Parent = GameObject };

			animHelper.IkLeftHand = _leftHandIk;
			animHelper.IkRightHand = _rightHandIk;
		}
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

			TryPickUp( holdable );
		}
	}

	private void UpdateHeldItemPositions()
	{
		if ( HeldItems.Count == 0 )
		{
			return;
		}

		var up = Vector3.Up;

		if ( Components.Get<CharacterController>() is { } charController )
		{
			up = (new Vector3( 0f, 0f, 300f ) + charController.Velocity).Normal;
		}

		var targetPos = Transform.Position + up * 64f;

		foreach ( var item in HeldItems )
		{
			var currentPos = item.Transform.Position;

			var accel = (targetPos - currentPos) * 250f;

			item.HeldVelocity = Vector3.Lerp( item.HeldVelocity, 0f, Helpers.Ease( 0.5f ) );
			item.HeldVelocity += accel * Time.Delta;

			item.Transform.Position += item.HeldVelocity * Time.Delta;

			targetPos = (targetPos + currentPos) * 0.5f + up * 16f;
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
