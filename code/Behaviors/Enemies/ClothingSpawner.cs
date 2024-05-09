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

	protected override void OnEnabled()
	{
		Apply();
	}

	private void Apply()
	{
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
	}
}
