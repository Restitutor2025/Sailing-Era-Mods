namespace Restitutor.RebalanceBoarding;

// Pure description of the single native edit (shared by the mod and the tests).
// BoatEntityBoardShoot.CheckProgressFull (RVA 0x25CD710, 0x44A bytes) compares
//   (own ShootProgress + target ShootProgress) with the constant 100:
//   +0x1BF  83 F9 64        cmp ecx, 0x64
//   +0x1C2  0F 8E ...       jle <no melee>
// Melee starts when the sum is greater than the constant. Only the imm8 at +0x1C1 changes.
// The same comparison is used for the player's flagship (scene melee) and for AI-only boarding
// (simulated melee); there is no other copy of this threshold.
public static class Patch
{
    public const string GameAssemblySha256 = "50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA";
    public const int MethodRva = 0x25CD710;
    public const int MethodLength = 0x44A;
    public const int CmpOffset = 0x1BF;          // 83 F9 imm8
    public const int ImmOffset = CmpOffset + 2;  // the byte we change
    public const byte OriginalThreshold = 100;
    public const byte NewThreshold = 40;

    /// Checks that a method body has exactly the expected shape around the edit site.
    public static bool BodyLooksRight(byte[] body) =>
        body.Length == MethodLength
        && body[CmpOffset] == 0x83 && body[CmpOffset + 1] == 0xF9
        && body[ImmOffset] == OriginalThreshold
        && body[ImmOffset + 1] == 0x0F && body[ImmOffset + 2] == 0x8E;

    /// Maps an RVA to a file offset using the PE section table in <paramref name="header"/> (first 4 KB of the file).
    public static long FileOffset(byte[] header, int rva)
    {
        int pe = BitConverter.ToInt32(header, 0x3C);
        int sections = BitConverter.ToUInt16(header, pe + 6), optional = BitConverter.ToUInt16(header, pe + 20);
        int table = pe + 24 + optional;
        for (int i = 0; i < sections; i++)
        {
            int s = table + i * 40;
            uint size = BitConverter.ToUInt32(header, s + 8), va = BitConverter.ToUInt32(header, s + 12), raw = BitConverter.ToUInt32(header, s + 20);
            if ((uint)rva >= va && (uint)rva < va + size) return (long)rva - va + raw;
        }
        return -1;
    }

    /// Expected body after the edit (for the restore guard).
    public static byte[] Patched(byte[] original)
    {
        var b = (byte[])original.Clone();
        b[ImmOffset] = NewThreshold;
        return b;
    }
}
