using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MelonLoader;

namespace Restitutor.RebalanceBoarding;

// 0.1.0 behaviour, unchanged: threshold 100 -> 40 in BoatEntityBoardShoot.CheckProgressFull by editing one
// byte of the running GameAssembly code, only when the GameAssembly SHA256 matches and the running method body
// equals the file bytes; restored on exit if it is still ours. No Harmony, no Restitutor.Core.
internal sealed class NativeThreshold
{
    private readonly MelonLogger.Instance log;
    public NativeThreshold(MelonLogger.Instance log) { this.log = log; }

    private IntPtr method;
    private byte[]? original;
    private bool ownsPatch;

    /// <summary>Threshold the running game actually compares against: 40 when our byte is in, 100 otherwise.</summary>
    public byte EffectiveThreshold => ownsPatch ? Patch.NewThreshold : Patch.OriginalThreshold;

    public void Apply()
    {
        try
        {
            if (!Environment.Is64BitProcess) throw new NotSupportedException("x64 only");
            var module = GetModuleHandleW("GameAssembly.dll");
            if (module == IntPtr.Zero) throw new InvalidOperationException("GameAssembly is not loaded");
            var path = new StringBuilder(32768);
            uint length = GetModuleFileNameW(module, path, path.Capacity);
            if (length == 0 || length >= path.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error());
            // The baseline body is read from the GameAssembly file itself once its SHA256 matches the known
            // version, so no game code is shipped inside this DLL.
            using (var file = File.OpenRead(path.ToString()))
            {
                using (var hash = SHA256.Create())
                    if (Convert.ToHexString(hash.ComputeHash(file)) != Patch.GameAssemblySha256)
                        throw new NotSupportedException("Unsupported GameAssembly SHA256; no patch applied");
                var header = new byte[4096];
                file.Position = 0; ReadExactly(file, header);
                long offset = Patch.FileOffset(header, Patch.MethodRva);
                if (offset < 0) throw new InvalidOperationException("CheckProgressFull RVA not in any section");
                original = new byte[Patch.MethodLength];
                file.Position = offset; ReadExactly(file, original);
            }
            if (!Patch.BodyLooksRight(original)) throw new InvalidOperationException("Unexpected baseline shape at edit site");
            method = IntPtr.Add(module, Patch.MethodRva);
            if (!ReadBody().SequenceEqual(original))
                throw new InvalidOperationException("CheckProgressFull already differs from baseline (another mod?); no patch applied");
            WriteThreshold(Patch.NewThreshold, true);
            log.Msg($"Rebalance Boarding {EntryPoint.Version} active: boarding melee starts when both ships' progress sum > {Patch.NewThreshold} (was {Patch.OriginalThreshold}). Applies to player and AI boarding (native byte, no hook). Save data unchanged.");
        }
        catch (Exception ex)
        {
            Restore();
            log.Error("Rebalance Boarding threshold patch failed (original threshold kept): " + ex);
        }
    }

    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = stream.Read(buffer, read, buffer.Length - read);
            if (n <= 0) throw new EndOfStreamException("GameAssembly shorter than expected");
            read += n;
        }
    }

    private byte[] ReadBody()
    {
        var bytes = new byte[original!.Length];
        Marshal.Copy(method, bytes, 0, bytes.Length);
        return bytes;
    }

    private void WriteThreshold(byte value, bool installing)
    {
        var site = IntPtr.Add(method, Patch.ImmOffset);
        if (!VirtualProtect(site, (UIntPtr)1, 0x40, out uint protection))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot unlock patch page");
        try
        {
            Marshal.WriteByte(site, value);
            ownsPatch = installing; // record ownership before anything else can fail
            if (!FlushInstructionCache(GetCurrentProcess(), site, (UIntPtr)1))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot flush instruction cache");
            if (Marshal.ReadByte(site) != value) throw new InvalidOperationException("Patch readback mismatch");
        }
        finally
        {
            if (!VirtualProtect(site, (UIntPtr)1, protection, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot restore page protection");
        }
    }

    public void Restore()
    {
        if (!ownsPatch || original == null) return;
        try
        {
            if (!ReadBody().SequenceEqual(Patch.Patched(original)))
            {
                log.Error("CheckProgressFull changed after our patch; refusing to overwrite another modification.");
                return;
            }
            WriteThreshold(Patch.OriginalThreshold, false);
            log.Msg("Rebalance Boarding: original threshold restored.");
        }
        catch (Exception ex) { log.Error("Rebalance Boarding restore failed: " + ex); }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetModuleFileNameW(IntPtr module, StringBuilder path, int size);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint protection, out uint old);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
