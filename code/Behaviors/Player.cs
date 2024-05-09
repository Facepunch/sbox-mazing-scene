using System;

namespace Mazing;

public sealed class Player : Component
{
	[RequireComponent] public Mazer Mazer { get; set; } = null!;
	[RequireComponent] public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[Property, Sync]
	public bool IsExiting { get; set; }

	[Property, Sync]
	public bool HasExited { get; set; }

	[Property] public event Action? Exiting;

	private bool _wasExiting;

	[Authority]
	public void UpdateClothing()
	{
		var clothing = ClothingContainer.CreateFromLocalUser();

		clothing.Apply( ModelRenderer );
	}

	protected override void OnUpdate()
	{
		if ( !_wasExiting && IsExiting )
		{
			_wasExiting = IsExiting;

			Exiting?.Invoke();
		}

		if ( IsProxy )
		{
			return;
		}

		if ( Mazer.State != MazerState.Falling )
		{
			return;
		}

		if ( !(Transform.Position.z < -64f) )
		{
			return;
		}

		IsExiting = true;

		if ( Transform.Position.z < -512f )
		{
			HasExited = true;
		}
	}

	[Authority( NetPermission.HostOnly )]
	public void Respawn( Vector3 pos )
	{
		Transform.Position = pos;

		Mazer.State = MazerState.Falling;

		_wasExiting = false;

		IsExiting = false;
		HasExited = false;

		UpdateClothing();
	}
}
