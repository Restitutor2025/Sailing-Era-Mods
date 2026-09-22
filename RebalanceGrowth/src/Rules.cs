namespace Restitutor.RebalanceGrowth;

// Pure rules (no game types) so tests/ can check them. Source facts: docs/mods/rebalance-growth/0.1.0.md.
public static class Rules {
    // GameConst keys, resolved from the getters' string literals (global-metadata v27):
    // UIHeroLevelUpModel.get_LevelGetSkill 0xB810C0 -> "GetSkillNeedLevelUp" (table 15),
    // get_MaxProp 0xB81010 and UICharacterModel.get_MaxPropValue 0x101CD90 -> "ROLE_PROP_MAX_COUNT" (table 99).
    public const string SkillIntervalKey="GetSkillNeedLevelUp";
    public const string StatMaxKey="ROLE_PROP_MAX_COUNT";
    public const int OriginalSkillInterval=15, SkillInterval=5;   // user: 15 -> 5
    public const int OriginalStatMax=99, StatMax=500;               // user: 99 -> 500
    public const int OriginalMaxLevel=99, MaxLevel=200;             // user: 99 -> 200 (RoleLevel row count)

    // RoleLevel row. Original table (R11, chunk 138) rows 2..99: exp = level*100, maxSkillCount 10,
    // skillPoint 1 when level%3==0, hpRatio 4, attackRatio 2 (row 1: skillPoint 0, ratios 0).
    // User: extend 100..200 by the same rule. EntryPoint checks rows 2..99 against this before adding.
    public readonly record struct Row(int Tid,int Level,int Exp,int MaxSkillCount,int SkillPoint,float HpRatio,float AttackRatio);
    public static Row RowFor(int level)=>new(level,level,level*100,10,level%3==0?1:0,4f,2f);

    // Levels per skill point: leaving level x grants a point when x % interval == 0 (original rule).
    // The 10-level button covers x = from .. from+count-1 but the original grants at most 1.
    public static int PointsInRange(int from,int count,int interval) {
        if(interval<=0 || count<=0) return 0;
        int n=0;
        for(int x=from;x<from+count;x++) if(x%interval==0) n++;
        return n;
    }

    // Level-up window "next skill point in N levels": inline loop in UIHeroLevelUpView.RefreshRoleData
    // (0xB84B3F..0xB84B8B) with a hardcoded 99: search x = level .. 98, diff = x+1-level, -1 if none.
    public static int OriginalNextDiff(int level,int interval) {
        if(interval<=0 || level+1>=100) return -1;
        for(int x=level;x<99;x++) if(x%interval==0) return x+1-level;
        return -1;
    }
    // Same search up to the real cap: x = level .. max-1.
    public static int NextDiff(int level,int max,int interval) {
        if(interval<=0) return -1;
        for(int x=level;x<max;x++) if(x%interval==0) return x+1-level;
        return -1;
    }
    // Rewrite the label only where the hardcoded 99 made the original say "no more points".
    public static bool FixNextLabel(int level,int max,int interval)=>OriginalNextDiff(level,interval)<0 && NextDiff(level,max,interval)>0;

    // Tab character tip (UICharacterView.RefreshTipsRoleInfo 0x102B460): isMax = (level==99),
    // need-exp text = "" when level>=99. Wrong only from level 99 up once the cap is higher.
    public static bool FixTip(int level)=>level>=OriginalMaxLevel;
    public static int TipMaxIndex(int level,int max)=>level>=max?1:0;
    public static bool TipShowsExp(int level,int max)=>level<max;

    // ---- 0.2.0 (user 2026-09-22) ----
    // Luck (id 6): display/selector value = stored + (level-1) x growth upValue (PlayerRoleData.GetLuckyValue 0x6798A0);
    // event checks read the stored value only (GetRolePropByID 0x676B30). Both now = growth-included value, capped at 99.
    public const int LuckyId=6, LuckyMax=99;
    public static int Lucky(int stored,int level,int upValue)=>Math.Min(LuckyMax,stored+Math.Max(0,level-1)*upValue);
    public static int CapLucky(int value)=>Math.Min(LuckyMax,value);

    // HP / attack gain per call (UpdateMaxHpValue 0x679E20, UpdateAttack 0x679F70):
    // gain = (int)(float)Math.Round((double)(((float)physical / D + 1f) * ratio)), D = 100 (original) -> 250 (user).
    public const float OriginalPhysicalDivisor=100f, PhysicalDivisor=250f;
    public static int Gain(int physical,float ratio,float divisor) {
        float x=(float)physical/divisor;
        x+=1f;
        x*=ratio;
        return (int)(float)Math.Round((double)x);
    }
    public static int GainCorrection(int physical,float ratio)=>Gain(physical,ratio,PhysicalDivisor)-Gain(physical,ratio,OriginalPhysicalDivisor);

    // Land exploration carry weight per navigator (PlayerRoleInfo.Weight = physical/3 + bonuses): final value capped at 30.
    public const float WeightMax=30f;
    public static float CapWeight(float w)=>w>WeightMax?WeightMax:w;
    // ---- 0.3.0 (user 2026-09-22) ----
    // Cumulative growth: every level adds the grade's per-mille to the stat's progress (stored in
    // PlayerRoleData.<Stat>_Exp, saved by Serialize 0x6780F0 / Deserialize 0x678940); each full 1000 = +1.
    // S 100% . A 77.5% . B 55% . C 32.5% . D 10% per level (equal steps, user). Progress is dropped at the cap.
    public const int GrowthUnit=1000;
    public static int RatePerMille(string letter)=>letter switch { "S"=>1000, "A"=>775, "B"=>550, "C"=>325, "D"=>100, _=>-1 };
    // RoleGrowthType.code is "role_growth__s" .. "role_growth__d"; ERoleGrowthType S=1 .. D=5 as fallback.
    public static string Letter(string? code,int tid) {
        if(!string.IsNullOrEmpty(code)) { char c=code[^1]; if(char.IsLetter(c)) return char.ToUpperInvariant(c).ToString(); }
        return tid is >=1 and <=5 ? "SABCD"[tid-1].ToString() : "?";
    }
    // levels level-ups from value cur with stored progress; returns the gain, progress after.
    public static int Grow(int cur,int progress,int rate,int levels,int max,out int progressAfter) {
        int gain=0; int p=Math.Clamp(progress,0,GrowthUnit-1);
        if(cur>=max) { progressAfter=0; return 0; }
        for(int i=0;i<levels;i++) {
            p+=rate;
            while(p>=GrowthUnit && cur+gain<max) { gain++; p-=GrowthUnit; }
            if(cur+gain>=max) { p=0; break; }
        }
        progressAfter=p;
        return gain;
    }

    // Level-up slider. Step k = level L+k -> L+k+1: Raw = experience the role receives (GetExp of the RoleLevel row;
    // step 0 minus the role's stored partial exp), Cost = what the pool (currency 2) pays (GetExpAfterBuff of Raw).
    public readonly record struct Step(int Raw,int Cost);
    // Amount -> whole levels + a partial remainder converted to raw exp (floor, always below the next step's Raw).
    public readonly record struct Spend(int Levels,long RawToRole,long Pay,int PartialRaw);
    public static Spend Plan(long amount,IReadOnlyList<Step> steps) {
        if(amount<=0) return new(0,0,0,0);
        long paid=0,raw=0; int n=0;
        while(n<steps.Count && paid+steps[n].Cost<=amount) { paid+=steps[n].Cost; raw+=steps[n].Raw; n++; }
        int partial=0; long rest=amount-paid;
        if(n<steps.Count && rest>0 && steps[n].Cost>0 && steps[n].Raw>0) {
            partial=(int)Math.Min(steps[n].Raw-1L,rest*steps[n].Raw/steps[n].Cost);
            if(partial>0) paid+=rest;          // the chosen amount is spent only when it buys something
        }
        return new(n,raw+partial,paid,partial);
    }
    // Pool needed to reach the max level (every remaining step); -1 when the list stopped early (pool ran out first).
    public static long ToMax(IReadOnlyList<Step> steps,bool complete) {
        if(!complete) return -1;
        long s=0; foreach(var x in steps) s+=x.Cost; return s;
    }
    // Slider value: clamped to the max usable pool only when it goes past it (user).
    public static long ClampChoice(long value,long pool,long toMax) {
        long v=Math.Clamp(value,0,Math.Max(0,pool));
        return toMax>=0 && v>toMax ? toMax : v;
    }
    // 0.3.0 (user, 2nd round): slider right end = what reaching the max level costs (or the whole pool if it
    // cannot reach it).
    public static long SliderMax(long pool,long toMax)=>toMax>=0 ? Math.Min(Math.Max(0,pool),toMax) : Math.Max(0,pool);
    public static string Num(int perMille)=>(perMille/10.0).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture);
    public static string Pct(int perMille)=>(perMille/10.0).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"%";
    // Level-up window, next to each ability: what the chosen amount does (growth is deterministic: preview = result).
    // "+1 · 32.5%→10%" (gain and progress before->after), "누적 32.5%" when no whole level is chosen, "최대" at the cap.
    public static string Preview(int cur,int stored,int rate,int levels,int max) {
        if(cur>=max) return "최대";
        int before=Math.Clamp(stored,0,GrowthUnit-1);
        if(levels<=0 || rate<0) return "누적 "+Pct(before);
        int gain=Grow(cur,stored,rate,levels,max,out int after);
        if(cur+gain>=max) return $"+{gain} · 최대";
        // Short form (user: fit beside the value): "+1 · 60→15%".
        return (gain>0?$"+{gain} · ":"")+$"{Num(before)}→{Pct(after)}";
    }
}
