using System.Diagnostics;
using MelonLoader;
using MelonLoader.Utils;

namespace Restitutor.CTRLInstant;

internal static class TraceLog
{
    private static StreamWriter? writer;
    private static MelonLogger.Instance? logger;
    private static readonly Stopwatch clock = Stopwatch.StartNew();
    private static int lines;
    internal static void Open(MelonLogger.Instance log)
    {
        logger = log;
        try
        {
            string directory = Path.Combine(MelonEnvironment.UserDataDirectory, "Restitutor", "CTRLInstant");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"CTRLInstant-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
            writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            log.Msg("0.1.5 diagnostic log: " + path);
            Write("startup", () => "version=0.1.5; NPC window keeps CTRL hold + deferred diagnostics");
        }
        catch (Exception ex) { log.Warning("Diagnostic file unavailable; using MelonLoader log: " + ex.Message); }
    }
    // Diagnostic failures must never alter input, pause ownership or exception flow.
    internal static void Write(string name, Func<string> detail)
    {
        try
        {
            if (lines >= 6000) return;
            string body;
            try { body = detail(); } catch (Exception ex) { body = "snapshot-error=" + ex.Message; }
            string line = $"{DateTime.Now:O} elapsedMs={clock.ElapsedMilliseconds} {name} {body}";
            if (++lines == 6000) line += " TRACE_LIMIT_REACHED restart for another recording";
            try { writer?.WriteLine(line); }
            catch { Close(); }
            logger?.Msg("[CTRLTrace] " + line);
        }
        catch { /* Read-only diagnostics cannot interrupt gameplay. */ }
    }
    internal static void Close()
    {
        try { writer?.Dispose(); } catch { }
        writer = null;
    }
}
