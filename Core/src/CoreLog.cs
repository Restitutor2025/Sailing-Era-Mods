using MelonLoader;

namespace Restitutor.Core;

internal static class CoreLog
{
    private static readonly MelonLogger.Instance log = new("Restitutor.Core");
    public static void Msg(string text) => log.Msg(text);
    public static void Error(string text) => log.Error(text);
}
