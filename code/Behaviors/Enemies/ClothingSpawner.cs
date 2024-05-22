using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mazing;

public sealed class ClothingSpawner : Component
{
	[RequireComponent]
	public SkinnedModelRenderer ModelRenderer { get; set; } = null!;

	[Property]
	public List<Clothing?> Clothing { get; set; } = new();

	[Property] public Color Color { get; set; } = Color.White;

	[Button( "Run", Icon = "checkroom" )]
	public void Apply()
	{
		using var _ = Scene.Push();

		var clothing = new ClothingContainer();

		foreach ( var item in Clothing )
		{
			if ( item is null )
			{
				continue;
			}

			clothing.Toggle( item );
		}

		clothing.Apply( ModelRenderer );

		foreach ( var tintable in ModelRenderer.Components.GetAll<ITintable>( FindMode.EverythingInDescendants ) )
		{
			if ( tintable is Component comp && comp.GameObject.Tags.Has( "clothing" ) )
			{
				tintable.Color = Color;
			}
		}
	}
}
