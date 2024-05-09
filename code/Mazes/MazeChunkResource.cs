using System;

namespace Mazing;

[Flags]
public enum MazeChunkFlags
{
	None = 0,
	IsLobby = 1
}

[GameResource( "Maze Chunk", "maze", "A hand-crafted segment of a maze.", Icon = "route" )]
public class MazeChunkResource : GameResource
{
	[Property] public MazeData Data { get; set; } = new();

	/// <summary>
	/// If true, this maze chunk won't be used for normal maze generation.
	/// </summary>
	[Property] public MazeChunkFlags Flags { get; set; }
}

internal static class SerializationExtensions
{
	public static string Serialize<T>( this IEnumerable<T> enumerable )
		where T : struct, Enum
	{
		return new string( enumerable
			.Select( x => (int)Convert.ChangeType( x, typeof(int) ) )
			.Select( x => (char)('0' + x) )
			.ToArray() );
	}

	public static IEnumerable<T>? Deserialize<T>( this string? value )
	{
		return value?
			.Select( x => x - '0' )
			.Select( x => Enum.ToObject( typeof(T), x ) )
			.OfType<T>();
	}
}
