using System.Globalization;
namespace Restitutor.Cheats.Character;

// Pure input/limit rules (no game types) so character-tests can run without the game.
internal static class Rules {
    // Ceiling for HP/attack/abilities. Below Int32.MaxValue so the game's own
    // "base + equipment/buff" additions (CurData+Buff, BaseData+effects) cannot overflow Int32.
    internal const int ValueMax=999_999_999;
    internal const int HpMaxMin=1;
    internal const int Digits=9;

    // Digits only, non-empty. Values beyond Int64 saturate.
    internal static bool TryDigits(string? text,out long value) {
        value=0;
        if(string.IsNullOrEmpty(text) || text.Any(c=>c<'0'||c>'9')) return false;
        string d=text.TrimStart('0');
        if(d.Length==0) return true;
        if(d.Length>18) { value=long.MaxValue; return true; }
        value=long.Parse(d,NumberStyles.None,CultureInfo.InvariantCulture); return true;
    }
    // Target for a row: digits only, clamped to [min,max]. Clamped reports whether it moved.
    internal static bool TryTarget(string? text,int min,int max,out int target,out bool clamped) {
        target=0;clamped=false;
        if(max<min || !TryDigits(text,out long v)) return false;
        long c=Math.Clamp(v,min,max);clamped=c!=v;target=(int)c;return true;
    }
    // Input-box correction: remove non-digits; above max becomes max.
    internal static string ClampInput(string? text,int max) {
        string digits=new((text??"").Where(c=>c>='0'&&c<='9').ToArray());
        if(digits.Length==0) return "";
        return TryDigits(digits,out long v) && v>max ? max.ToString(CultureInfo.InvariantCulture) : digits;
    }
    // Skill level change: raising uses the game's own level-up routine (delta>0);
    // lowering sets the base level directly (the game has no lowering routine).
    internal static (bool raise,int delta) SkillPlan(int before,int target)=>(target>before,target-before);

    // Layout 1.1.0: 640 wide, two columns of 310. Entries = 9 value rows + skill header + skills (min 1 line),
    // filled top-to-bottom in the left column first; the left column takes the extra entry when odd.
    internal const float PanelWidth=640,ColumnWidth=310,ColumnGap=20,RowsTop=66,RowHeight=34,Footer=36;
    internal const int FieldRows=9;
    internal static int Entries(int skills)=>FieldRows+1+Math.Max(1,skills);
    internal static int RowsPerColumn(int skills)=>(Entries(skills)+1)/2;
    internal static (float x,float y) Slot(int index,int skills) {
        int per=RowsPerColumn(skills);
        return (index<per?0:ColumnWidth+ColumnGap,(index<per?index:index-per)*RowHeight);
    }
    internal static float Height(int skills)=>RowsTop+RowsPerColumn(skills)*RowHeight+Footer;

    // Absolute set: at most one write, read back, no retry.
    internal static int Apply(int target,Func<int> read,Action<int> write) {
        int before=read();
        if(before==target) return before;
        write(target);
        int after=read();
        if(after!=target) throw new InvalidOperationException($"mismatch target={target} actual={after}; no retry");
        return after;
    }
}
