using Restitutor.RebalanceLandExplore;

int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }

// The original roll, reproduced exactly: Random.Next(0,100) yields 0..99, success when value <= threshold.
static int SuccessesOutOf100(int threshold) => Enumerable.Range(0, 100).Count(v => v <= threshold);

Check(Rules.MaxRatio == 50, "cap is 50");

// 1. Hard clamp: everything above 50 becomes 50, nothing below moves.
Check(Rules.Cap(0) == 0 && Rules.Cap(49) == 49 && Rules.Cap(50) == 50, "at or below the cap is unchanged");
Check(Rules.Cap(51) == 50 && Rules.Cap(79) == 50 && Rules.Cap(100) == 50, "above the cap becomes 50");
Check(Enumerable.Range(0, 101).All(r => Rules.Cap(r) <= Rules.MaxRatio), "never above the cap");
Check(Enumerable.Range(0, 101).All(r => Rules.Cap(r) <= r), "the cap never raises a chance");

// 2. Off-by-one fix: the number shown is the real chance, for every possible ratio.
Check(Enumerable.Range(0, 101).All(r => SuccessesOutOf100(Rules.RollThreshold(r)) == Rules.Cap(r)),
      "real chance == shown number, 0..100");
Check(Rules.RollThreshold(50) == 49 && Rules.RollThreshold(30) == 29 && Rules.RollThreshold(0) == -1,
      "threshold is one below the shown number");
Check(Rules.Chance(0) == 0 && SuccessesOutOf100(Rules.RollThreshold(0)) == 0, "0% is a certain failure");
Check(Rules.Chance(50) == 50 && Rules.Chance(79) == 50 && Rules.Chance(100) == 50, "capped chances are 50%");
Check(Rules.Chance(30) == 30 && Rules.Chance(1) == 1, "uncapped chances are unchanged");

// 3. What the original did, for the record: one percentage point more than it showed.
Check(Enumerable.Range(0, 100).All(r => Rules.OriginalChance(r) == SuccessesOutOf100(r)), "original chance model");
Check(Rules.OriginalChance(0) == 1 && Rules.OriginalChance(79) == 80 && Rules.OriginalChance(100) == 100,
      "original: shown 0 -> 1%, 79 -> 80%");
Check(Enumerable.Range(0, 101).All(r => Rules.Chance(r) <= Rules.OriginalChance(r)), "never more generous than the original");

// 4. Logging helper.
Check(!Rules.NeedsCap(50) && Rules.NeedsCap(51) && !Rules.NeedsCap(0), "NeedsCap only above the cap");

// 5. Coin chances the reachable range produces (prop < required, so p < 0.8 -> shown <= 79).
foreach (var (prop, required) in new[] { (20, 98), (69, 98), (97, 98), (49, 50), (1, 100) })
{
    int ratio = (int)(Math.Clamp((float)prop / required - 0.2f, 0f, 1f) * 100);
    Check(Rules.Chance(ratio) == Math.Min(ratio, 50), $"stat {prop}/{required} -> shown {ratio}% -> real {Rules.Chance(ratio)}%");
}

Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
