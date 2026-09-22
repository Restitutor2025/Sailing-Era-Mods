using System.Globalization;
namespace Restitutor.StatRank;

// Level-up stat roll, from UIHeroLevelUpCtrl.GetResult 0xB7FDF0 / GetResultByRate 0xB7FFC0:
// Random.Range(0,100) <= rate succeeds, so the chance is (rate+1)%, capped at 100%.
// perfectRate is rolled first (+2); on failure successRate (+1); otherwise +0.
internal static class Rules {
    internal static double Chance(int rate)=>Math.Clamp(rate+1,0,100)/100.0;
    internal static (double plus2,double plus1,double plus0,double mean) Odds(int successRate,int perfectRate) {
        double p2=Chance(perfectRate), p1=(1-p2)*Chance(successRate), p0=1-p2-p1;
        return (p2,p1,p0,2*p2+p1);
    }
    // RoleGrowthType.code is "role_growth__s" .. "role_growth__d".
    internal static string Letter(string? code,int tid) {
        if(!string.IsNullOrEmpty(code)) { char c=code[^1]; if(char.IsLetter(c)) return char.ToUpperInvariant(c).ToString(); }
        return tid is >=1 and <=5 ? "SABCD"[tid-1].ToString() : "?";
    }
    internal static string Percent(double p)=>(p*100).ToString("0.#",CultureInfo.InvariantCulture)+"%";
    internal static string Mean(double m)=>m.ToString("0.00",CultureInfo.InvariantCulture);
    internal static readonly string[] Header={"등급","+2","+1","+0","레벨당 평균"};
    internal static string[] Row(string letter,int successRate,int perfectRate) {
        var o=Odds(successRate,perfectRate);
        return new[]{letter,Percent(o.plus2),Percent(o.plus1),Percent(o.plus0),Mean(o.mean)};
    }
    // 0.2.0: with Restitutor Rebalance Growth 0.3.0+ active the roll is replaced by cumulative growth:
    // per level S 100 / A 77.5 / B 55 / C 32.5 / D 10 % (same table as RebalanceGrowth Rules.RatePerMille),
    // progress kept in PlayerRoleData.<Stat>_Exp as per-mille, dropped at the ability cap.
    internal static int RatePerMille(string letter)=>letter switch { "S"=>1000, "A"=>775, "B"=>550, "C"=>325, "D"=>100, _=>-1 };
    internal static string PerMille(int v)=>(v/10.0).ToString("0.#",CultureInfo.InvariantCulture)+"%";
    internal static readonly string[] GrowthHeader={"등급","레벨당","누적"};
    internal static string[] GrowthRow(string letter,int progress,bool atCap) {
        int rate=RatePerMille(letter);
        return new[]{letter,rate<0?"?":PerMille(rate),atCap?"최대":PerMille(Math.Clamp(progress,0,999))};
    }
}
