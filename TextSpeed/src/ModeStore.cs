using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Restitutor.TextSpeed;

public enum TextMode { Slow = 0, Instant = 1 }

// One persisted enum is the only source of truth. UI checkboxes never own state.
public sealed class ModeStore
{
    private readonly string path;
    private byte[]? expected;
    public TextMode Current { get; private set; }
    public bool Ready { get; private set; }
    public string? Error { get; private set; }
    public bool RecoveredBackup { get; private set; }
    public ModeStore(string path) { this.path = path; Load(); }

    private sealed record Record(int Schema, string Mode, string Checksum);
    private static string Sum(string mode) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Restitutor.TextSpeed/v1/" + mode)));
    private static byte[] Encode(TextMode mode)
    {
        var text = mode == TextMode.Slow ? "slow" : "instant";
        return JsonSerializer.SerializeToUtf8Bytes(new Record(1, text, Sum(text)));
    }
    private static TextMode Decode(byte[] bytes)
    {
        var r = JsonSerializer.Deserialize<Record>(bytes) ?? throw new InvalidDataException("Empty settings.");
        if (r.Schema != 1 || (r.Mode != "slow" && r.Mode != "instant") || r.Checksum != Sum(r.Mode))
            throw new InvalidDataException("Invalid text-speed settings/checksum.");
        return r.Mode == "slow" ? TextMode.Slow : TextMode.Instant;
    }
    private void Load()
    {
        try
        {
            if (!File.Exists(path) && !File.Exists(path + ".bak"))
            {
                Current = TextMode.Slow;
                Commit(Encode(Current), false);
            }
            else
            {
                try { expected = File.ReadAllBytes(path); Current = Decode(expected); }
                catch
                {
                    // Never silently rewrite damaged user data or fall back to the default.
                    var backup = File.ReadAllBytes(path + ".bak");
                    Current = Decode(backup);
                    Error = "Primary settings invalid; backup value displayed read-only. Restore the file before changing options.";
                    RecoveredBackup = true;
                    return;
                }
            }
            Ready = true;
        }
        catch (Exception ex) { Error = ex.Message; Ready = false; }
    }

    // Only the option click handlers call this in production. Commit before publishing.
    public bool SelectFromOptions(TextMode value)
    {
        if (!Ready) return false;
        if (!Enum.IsDefined(typeof(TextMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (value == Current) return true;
        try
        {
            var disk = File.ReadAllBytes(path);
            if (expected == null || !disk.AsSpan().SequenceEqual(expected))
                throw new IOException("Settings changed outside the options window; refusing to overwrite.");
            var bytes = Encode(value);
            Commit(bytes, true);
            Current = value;
            Error = null;
            return true;
        }
        catch (Exception ex) { Error = ex.Message; return false; }
    }
    private void Commit(byte[] bytes, bool replace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var f = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { f.Write(bytes); f.Flush(true); }
            if (replace) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
            expected = bytes;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
