
namespace Mazing;

public class Treasure : Component
{
	[RequireComponent] public Throwable Throwable { get; set; } = null!;
	[Property] public int Value { get; set; } = 1;

	private bool _scored = false;

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}

		if ( Throwable.IsExiting && !_scored )
		{
			_scored = true;
			Scene.Components.Get<MazingGame>( FindMode.Enabled | FindMode.InChildren ).Score += Value;
		}

		if ( _scored && WorldPosition.z <= -512f )
		{
			GameObject.Destroy();
		}
	}
}

public record TreasureInfo( int Value );
