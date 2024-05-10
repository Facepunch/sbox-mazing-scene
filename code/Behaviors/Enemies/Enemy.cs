using System.Text.Json;

namespace Mazing;

public sealed class Enemy : Component
{
	[Property] public int Difficulty { get; set; } = 1;
	[Property] public int MinLevel { get; set; } = 0;
}

public record EnemyInfo( int Difficulty, int MinLevel );
