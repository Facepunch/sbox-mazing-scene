
namespace Mazing;

public class LocalMazerTest : Component
{
	[RequireComponent] public Mazer Mazer { get; set; } = null!;

	protected override void OnUpdate()
	{
		Mazer.MoveInput = Input.AnalogMove;
	}
}
