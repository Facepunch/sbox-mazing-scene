using Sandbox;

namespace Mazing;

public sealed class Key : Component
{
	[RequireComponent]
	public Holdable Holdable { get; set; } = null!;
}
