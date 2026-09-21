using MelonLoader;

namespace Restitutor.Core;

/// <summary>Core version and the minimum-version check every mod runs at start.</summary>
public static class CoreInfo
{
    /// <summary>Semantic version of this Core. A property, not a const: a const would be
    /// copied into each mod at build time and always report the version the mod was built with.</summary>
    public static string Version => "0.1.0";

    /// <summary>True when this Core is at least <paramref name="minimum"/> (x.y.z).</summary>
    public static bool Satisfies(string minimum) => VersionText.AtLeast(Version, minimum);

    /// <summary>Logs one line and returns false when this Core is older than the mod needs.
    /// The mod then leaves itself disabled; nothing else is affected.</summary>
    public static bool Require(MelonLogger.Instance log, string minimum)
    {
        if (Satisfies(minimum)) return true;
        log.Error($"Restitutor.Core {Version} is older than required {minimum}; this mod stays disabled. Install the matching Restitutor.Core.dll in UserLibs.");
        return false;
    }
}
