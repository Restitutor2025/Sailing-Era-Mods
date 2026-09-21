using MelonLoader;
[assembly: MelonInfo(typeof(Restitutor.TextSpeed.TextSpeedMod), "Restitutor text speed", "0.1.6", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]
namespace Restitutor.TextSpeed;
// The only file to omit when merging into a future MelonMod entry point.
public sealed class TextSpeedMod : MelonMod
{
    public override void OnInitializeMelon() => TextSpeedModule.Initialize(LoggerInstance);
    public override void OnLateUpdate() => TextSpeedModule.LateUpdate();
    public override void OnDeinitializeMelon() => TextSpeedModule.Shutdown();
}
