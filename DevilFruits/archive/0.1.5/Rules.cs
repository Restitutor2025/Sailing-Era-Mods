namespace Restitutor.DevilFruits;

// Pure rules (no game types) so tests/ can compile this file alone.
// Growth grade = Gyyx.Template.Hero.<stat>Growth = RoleGrowthType tid: 1=S 2=A 3=B 4=C 5=D.
internal static class Rules {
    internal const int ItemTypeTid=9900;          // new ItemType row (unused in the R11 table)
    internal const int ParentItemType=1000;       // "소모품"
    internal const int FirstItemTid=990001;       // 990001..990005 (unused in the R11 table)
    internal const int IconSourceItem=23207;      // 대추야자: icon / frame / weight source (user choice)
    internal const string Icon="Assets/AssetsPackage/Img/ItemIcon/icon_goods_23207.png";

    // Order = UICharacterView rows Physical, Perceive, Craft, Knowledge, Charm (= Stat Rank order).
    internal static readonly string[] StatNames={"신체","감지","기교","학식","매력"};
    internal static int Count=>StatNames.Length;
    internal static int ItemTid(int stat)=>FirstItemTid+stat;
    internal static int StatOf(int itemTid)=>itemTid-FirstItemTid is var s && s>=0 && s<Count ? s : -1;

    internal static string NameKey(int stat)=>"Item_Name_"+ItemTid(stat);
    internal static string DescKey(int stat)=>"Item_Des_"+ItemTid(stat);
    internal static string LongDescKey(int stat)=>"Item_LongDes_"+ItemTid(stat);
    internal const string TypeNameKey="ItemType_Name_DevilFruit";
    internal const string TypeHintKey="Item_Tips_DevilFruit";

    internal static string ItemName(int stat)=>StatNames[stat]+" 악마의 열매";
    internal static string ItemDesc(int stat)=>"먹은 항해사의 "+StatNames[stat]+" 성장 등급이 한 단계 오른다.";
    internal const string TypeName="악마의 열매";
    internal const string TypeHint="*인물 화면에서 해당 능력의 등급 글자를 클릭해 사용";

    // All texts this mod adds to TextLib, key -> Korean text.
    internal static IEnumerable<(string key,string text)> Texts() {
        for(int s=0;s<Count;s++) {
            yield return (NameKey(s),ItemName(s));
            yield return (DescKey(s),ItemDesc(s));
            yield return (LongDescKey(s),ItemDesc(s));
        }
        yield return (TypeNameKey,TypeName);
        yield return (TypeHintKey,TypeHint);
    }

    // Saved record: one ItemTid(stat) entry in PlayerRoleData.ListSkillBooks per fruit eaten.
    internal static int[] Steps(IEnumerable<int> listSkillBooks) {
        var steps=new int[Count];
        foreach(var id in listSkillBooks) { int s=StatOf(id); if(s>=0) steps[s]++; }
        return steps;
    }
    // Grade to write into the template: original minus eaten fruits, never better than S.
    internal static int Applied(int original,int steps)=>Math.Max(1,original-Math.Max(0,steps));
    internal static bool IsTop(int grade)=>grade<=1;

    internal static string Letter(string? code,int grade) {
        if(!string.IsNullOrEmpty(code)) { char c=code[^1]; if(char.IsLetter(c)) return char.ToUpperInvariant(c).ToString(); }
        return grade is >=1 and <=5 ? "SABCD"[grade-1].ToString() : "?";
    }
}
