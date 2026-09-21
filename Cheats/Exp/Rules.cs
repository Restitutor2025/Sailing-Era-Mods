using System.Globalization;
namespace Restitutor.Cheats.Exp;
internal static class Rules {
    // Experience is currency ID 2 (IncreaseExpAmount RVA 0x452A30 passes 2).
    internal const int CurrencyId=2;
    // ModifyAmount(int,int) RVA 0x452AC0 takes Int32 and rejects negatives: Int32.MaxValue is the writable ceiling.
    internal const int Max=int.MaxValue;
    internal const string Hint="0~2,147,483,647 정수 · 초과는 최대치로 보정";
    // Digits only. No sign, no separators, no spaces.
    internal static bool TryTarget(string text,out int value) {
        value=0;
        if(string.IsNullOrEmpty(text) || text.Any(c=>c<'0'||c>'9')) return false;
        value=Clamp(text);return true;
    }
    // Any digit string above Max (including values beyond Int64) becomes Max.
    internal static int Clamp(string digits) {
        string d=digits.TrimStart('0');
        if(d.Length==0) return 0;
        if(d.Length>10 || (d.Length==10 && string.CompareOrdinal(d,"2147483647")>0)) return Max;
        return int.Parse(d,NumberStyles.None,CultureInfo.InvariantCulture);
    }
    // Input-box correction. Non-digit text is removed; above-Max text is replaced by Max.
    internal static string ClampInput(string text) {
        string digits=new(text.Where(c=>c>='0'&&c<='9').ToArray());
        if(digits.Length==0) return "";
        return Clamp(digits)==Max && digits.TrimStart('0')!="2147483647" ? Max.ToString(CultureInfo.InvariantCulture) : digits;
    }
    // Absolute set. One write at most; no retry on rejection or mismatch.
    internal static long Apply(int target,Func<long> read,Func<int,bool> write) {
        if(target<0) throw new ArgumentOutOfRangeException(nameof(target));
        long before=read();
        if(before==target) return before;
        if(!write(target)) throw new InvalidOperationException("Native exp update rejected; no retry");
        long after=read();
        if(after!=target) throw new InvalidOperationException($"Exp mismatch target={target} actual={after}; no retry");
        return after;
    }
}
