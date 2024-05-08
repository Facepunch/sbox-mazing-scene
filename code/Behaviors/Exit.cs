using Sandbox;

namespace Mazing;

public sealed class Exit : Component
{
	[RequireComponent]
	public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[RequireComponent]
	public MazeObject MazeObject { get; set; } = null!;

	[RequireComponent]
	public Collider Collider { get; set; } = null!;

	[Property]
	public Collider Trigger { get; set; } = null!;

	[Property]
	public bool IsOpen { get; set; }

	public void Open( Key key )
	{
		IsOpen = true;

		ModelRenderer.SceneModel.SetAnimParameter( "open", true );
		Collider.Enabled = false;

		if ( key.Components.Get<Holdable>() is {} holdable )
		{
			holdable.Drop();
		}

		key.GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		foreach ( var collider in Trigger.Touching )
		{
			if ( collider.Components.Get<Key>() is { } key )
			{
				Open( key );
				continue;
			}

			if ( collider.Components.Get<Holder>() is { HeldItem: {} heldItem } )
			{
				if ( heldItem.Components.Get<Key>() is { } heldKey )
				{
					Open( heldKey );
					continue;
				}
			}
		}
	}
}
