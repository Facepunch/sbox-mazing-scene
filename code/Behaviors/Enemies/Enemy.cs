using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mazing;

public sealed class Enemy : Component
{
	[Property] public int Difficulty { get; set; } = 1;
	[Property] public int MinLevel { get; set; } = 0;
}

