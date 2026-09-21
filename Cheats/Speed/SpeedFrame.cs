namespace Restitutor.Cheats.Speed;

// A call-local override: no speed value is stored in PlayerData or a save file.
internal readonly struct SpeedFrame {
    internal readonly float Original, Applied;
    internal readonly bool Changed;
    internal SpeedFrame(float original, int multiplier) {
        Original = original;
        Applied = original * multiplier;
        Changed = multiplier >= 2 && multiplier <= 5 && original > 0 &&
            float.IsFinite(original) && float.IsFinite(Applied);
    }
    internal float Restore(float current) => Changed && current == Applied ? Original : current;
}
