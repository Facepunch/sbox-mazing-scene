using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mazing;

public class Treasure : Component
{
	[Property] public int Value { get; set; } = 5;
}

public record TreasureInfo( int Value );
