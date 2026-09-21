using System.Globalization;
namespace Restitutor.Cheats.Money;
internal static class Rules {
    internal const string Hint="0~2,147,483,647 · 적용으로 확정";
    internal static bool TryTarget(string text,out int value)=>int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value)&&value>=0;
    internal static string ClampInput(string text) {
        if(text.Length==0 || text.Any(c=>c<'0'||c>'9')) return text;
        string digits=text.TrimStart('0');
        return digits.Length>10 || (digits.Length==10 && string.CompareOrdinal(digits,"2147483647")>0) ? "2147483647" : text;
    }
    internal static long Apply(int target,Func<long> read,Func<int,bool> write) {
        if(target<0) throw new ArgumentOutOfRangeException(nameof(target));
        long before=read();
        if(before==target) return before;
        if(!write(target)) throw new InvalidOperationException("Native money update rejected; no retry");
        long after=read();
        if(after!=target) throw new InvalidOperationException($"Money mismatch target={target} actual={after}; no retry");
        return after;
    }
}
