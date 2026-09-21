using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MelonLoader;

[assembly: MelonInfo(typeof(Restitutor.FleetInfo.EntryPoint), "Restitutor fixes Fleet Info", "0.1.0", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.FleetInfo;

public sealed class EntryPoint : MelonMod
{
    private const string Baseline = "50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private const int MethodRva = 0xB08DC0;
    private const int BehaviorOffset = 0x3B3; // RVA B09173: immediate in `mov r9b,2`.
    private IntPtr method;
    private byte[]? original;
    private bool ownsPatch;

    public override void OnInitializeMelon()
    {
        try
        {
            if (!Environment.Is64BitProcess) throw new NotSupportedException("x64 only");
            var module = GetModuleHandleW("GameAssembly.dll");
            if (module == IntPtr.Zero) throw new InvalidOperationException("GameAssembly is not loaded");
            var path = new StringBuilder(32768);
            uint length = GetModuleFileNameW(module, path, path.Capacity);
            if (length == 0 || length >= path.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error());
            using (var file = File.OpenRead(path.ToString()))
            using (var hash = SHA256.Create())
                if (Convert.ToHexString(hash.ComputeHash(file)) != Baseline)
                    throw new NotSupportedException("Unsupported GameAssembly SHA256; no patch applied");
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("FleetInfo.ExpectedBody")
                ?? throw new InvalidOperationException("Missing expected native body");
            using var buffer = new MemoryStream();
            resource.CopyTo(buffer);
            original = buffer.ToArray();
            if (original.Length != 0x962 || original[BehaviorOffset] != 2)
                throw new InvalidOperationException("Invalid embedded baseline");
            method = IntPtr.Add(module, MethodRva);
            if (!ReadBody().SequenceEqual(original))
                throw new InvalidOperationException("Fleet method already differs from baseline; no patch applied");
            // Only this call site's insertion policy changes. None=0 returns false on
            // duplicate; the original caller ignores the result and continues its loop.
            WriteBehavior(0, true);
            LoggerInstance.Msg("Fleet Info 0.1.0 active: duplicate owners keep the first UI lookup entry. Save data and fleet instances unchanged.");
        }
        catch (Exception ex)
        {
            Restore();
            LoggerInstance.Error("Fleet Info initialization failed: " + ex);
        }
    }

    private byte[] ReadBody()
    {
        var bytes = new byte[original!.Length];
        Marshal.Copy(method, bytes, 0, bytes.Length);
        return bytes;
    }

    private void WriteBehavior(byte value, bool installing)
    {
        var site = IntPtr.Add(method, BehaviorOffset);
        if (!VirtualProtect(site, (UIntPtr)1, 0x40, out uint protection))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot unlock patch page");
        try
        {
            Marshal.WriteByte(site, value);
            ownsPatch = installing; // Record ownership before any later operation can fail.
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

    private void Restore()
    {
        if (!ownsPatch || original == null) return;
        try
        {
            var expected = (byte[])original.Clone(); expected[BehaviorOffset] = 0;
            if (!ReadBody().SequenceEqual(expected))
            {
                LoggerInstance.Error("Fleet method changed after our patch; refusing to overwrite another modification.");
                return;
            }
            WriteBehavior(2, false);
            LoggerInstance.Msg("Fleet Info native instruction restored.");
        }
        catch (Exception ex) { LoggerInstance.Error("Fleet Info restore failed: " + ex); }
    }

    public override void OnDeinitializeMelon() => Restore();

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
