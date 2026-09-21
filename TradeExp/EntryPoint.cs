using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MelonLoader;

[assembly: MelonInfo(typeof(Restitutor.TradeExp.EntryPoint), "Restitutor fixes trade exp", "0.1.1", "Restitutor")]
[assembly: MelonGame("bolingo", "SailingEra")]

namespace Restitutor.TradeExp;

public sealed class EntryPoint : MelonMod
{
    private const string Baseline = "50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    private sealed class Site
    {
        public readonly string Name;
        public readonly int Rva, Length, Offset, PatchLength;
        public IntPtr Address;
        public byte[] Original = Array.Empty<byte>();
        public byte[] Patched = Array.Empty<byte>();
        public bool Owned;
        public Site(string name, int rva, int length, int offset, int patchLength)
            => (Name, Rva, Length, Offset, PatchLength) = (name, rva, length, offset, patchLength);
    }
    private readonly Site[] sites = {
        new("AccountRefresh", 0x11AE200, 0x3322, 0x19A0, 0x49),
        new("AccountRefreshCallback", 0x4438A0, 0x6DA, 0x2D1, 0x3E)
    };

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

            // Validate BOTH full method bodies before changing either instruction.
            foreach (var site in sites)
            {
                using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "Restitutor_fixes_trade_exp.evidence." + site.Name + ".bin")
                    ?? throw new InvalidOperationException("Missing baseline: " + site.Name);
                using var buffer = new MemoryStream();
                resource.CopyTo(buffer);
                site.Original = buffer.ToArray();
                using var patchedResource = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "Restitutor_fixes_trade_exp.evidence." + site.Name + ".patched.bin")
                    ?? throw new InvalidOperationException("Missing patched body: " + site.Name);
                using var patchedBuffer = new MemoryStream();
                patchedResource.CopyTo(patchedBuffer);
                site.Patched = patchedBuffer.ToArray();
                if (site.Patched.Length != site.Length ||
                    !site.Patched.Take(site.Offset).SequenceEqual(site.Original.Take(site.Offset)) ||
                    !site.Patched.Skip(site.Offset + site.PatchLength).SequenceEqual(site.Original.Skip(site.Offset + site.PatchLength)))
                    throw new InvalidOperationException("Invalid patched body: " + site.Name);
                site.Address = IntPtr.Add(module, site.Rva);
                if (site.Original.Length != site.Length)
                    throw new InvalidOperationException("Invalid embedded baseline: " + site.Name);
                if (!ReadBody(site).SequenceEqual(site.Original))
                    throw new InvalidOperationException("Native method differs from baseline: " + site.Name);
            }
            foreach (var site in sites) Write(site, true);
            LoggerInstance.Msg("Trade exp 0.1.1 active: market balance display saturates at 2,147,483,647 with overflow-safe Int64 addition. Experience awards and saves unchanged.");
        }
        catch (Exception ex)
        {
            Restore();
            LoggerInstance.Error("Trade exp initialization failed: " + ex);
        }
    }

    private static byte[] ReadBody(Site site)
    {
        var bytes = new byte[site.Length];
        Marshal.Copy(site.Address, bytes, 0, bytes.Length);
        return bytes;
    }

    private static void Write(Site site, bool installing)
    {
        var address = IntPtr.Add(site.Address, site.Offset);
        if (!VirtualProtect(address, (UIntPtr)site.PatchLength, 0x40, out uint protection))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot unlock patch page");
        try
        {
            // All bytes are prevalidated before installation; only display arithmetic is replaced.
            var body = installing ? site.Patched : site.Original;
            site.Owned = true; // Retain ownership if writing/flushing fails.
            Marshal.Copy(body, site.Offset, address, site.PatchLength);
            if (!FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)site.PatchLength))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot flush instruction cache");
            if (!ReadBody(site).SequenceEqual(body)) throw new InvalidOperationException("Patch readback mismatch");
            site.Owned = installing;
        }
        finally
        {
            if (!VirtualProtect(address, (UIntPtr)site.PatchLength, protection, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot restore page protection");
        }
    }

    private void Restore()
    {
        foreach (var site in sites.Reverse())
        {
            if (!site.Owned) continue;
            try
            {
                var expected = site.Patched;
                if (!ReadBody(site).SequenceEqual(expected))
                    throw new InvalidOperationException("Method changed after our patch; refusing to overwrite: " + site.Name);
                Write(site, false);
            }
            catch (Exception ex) { LoggerInstance.Error("Trade exp restore failed: " + ex); }
        }
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
