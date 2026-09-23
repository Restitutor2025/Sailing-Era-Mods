using Restitutor.RebalanceNpcFleets;

int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }

// Original NPC_BUY_SHIP_LIST (table SHA 8be62de8…).
const string orig = "120|140|142|141|150|160|161|170|180|181|182|190|191|192|200|201|202|260|110|210|211|212|220|221|130|270|271|272|280|281|310|290|320|300|293|230|231|240|241|250|251";
var got = Rules.BuyListWithout(orig);
Check(got != null && got.Split('|').Length == 40, "41 -> 40");
Check(got != null && !got.Split('|').Contains("300"), "300 gone");
Check(got == orig.Replace("|300|", "|"), "order kept");
Check(Rules.BuyListWithout(got) == null, "second pass unchanged");
Check(Rules.BuyListWithout("3000|1300|300") == "3000|1300", "only exact token");
Check(Rules.BuyListWithout("300") == null, "never empties");
Check(Rules.BuyListWithout("") == null && Rules.BuyListWithout(null) == null, "empty/null");
Check(Rules.BuyListWithout("300|120") == "120" && Rules.BuyListWithout("120|300") == "120", "ends");

// Teams: originals from the table census.
var big = new HashSet<int>{300, 2006, 3300, 306, 307};
Check(Rules.Teams.Length == 7, "7 fleets");
foreach (var t in Rules.Teams)
{
    Check(Rules.Check(t, t.Flag, t.Others) == Rules.Result.Changed, $"{t.Tid} original -> change");
    Check(Rules.Check(t, t.NewFlag, t.NewOthers) == Rules.Result.AlreadyChanged, $"{t.Tid} applied -> ok");
    Check(Rules.Check(t, 110, t.Others) == Rules.Result.Differs, $"{t.Tid} edited -> untouched");
    Check(!big.Contains(t.NewFlag) && !t.NewOthers.Any(big.Contains), $"{t.Tid} no large Fu left");
    Check(t.NewOthers.Length == t.Others.Length, $"{t.Tid} same ship count");
    // Only large-Fu slots change.
    Check(big.Contains(t.Flag) || t.Flag == t.NewFlag, $"{t.Tid} flag");
    for (int i = 0; i < t.Others.Length; i++) Check(big.Contains(t.Others[i]) || t.Others[i] == t.NewOthers[i], $"{t.Tid} slot {i}");
    if (t.Pirate)
    {
        Check(t.NewFlag == Rules.Ganzeng, $"{t.Tid} pirate flag 간증선");
        for (int i = 0; i < t.Others.Length; i++) if (big.Contains(t.Others[i])) Check(t.NewOthers[i] is Rules.Haicang or Rules.Kailang, $"{t.Tid} pirate escort warship");
    }
    else
    {
        Check(t.NewFlag == Rules.Fu && t.NewOthers.All(x => x == Rules.Fu), $"{t.Tid} trade fleet all 복선");
    }
}
Check(Rules.Teams.Count(t => t.Pirate) == 5, "5 pirate fleets");

Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
