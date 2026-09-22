using Restitutor.RebalanceShipyard;

// Rule checks. Ship.Technical values from the game table (SHA256 8be62de8…), ShipYard.BuyShip (same for all 43 yards).
int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }
string S(List<int>? l) => l == null ? "null" : "[" + string.Join(",", l) + "]";

var tech = new Dictionary<int, int> {
    [110]=0,[120]=0,[130]=0,[140]=100,[160]=300,[180]=500,[190]=700,[200]=500,[210]=200,[220]=300,
    [230]=300,[240]=600,[250]=900,[260]=800,[270]=200,[280]=400,[290]=500,[300]=700,[310]=300,[320]=500 };
int? T(int id) => tech.TryGetValue(id, out var t) ? t : null;

// Real per-port original results at max development (table calculation) -> expected.
var cases = new (int[] input, int[]? expect)[] {
    (new[]{120,140,160,180,190,260,250}, new[]{120}),          // London
    (new[]{140,160,180,190,260,110}, new[]{110}),              // Seville
    (new[]{110,130,210,280}, new[]{110,130}),                  // Malacca
    (new[]{130,270,280,290,300}, new[]{130}),                  // Hangzhou
    (new[]{130,310,320}, new[]{130}),                          // Nagasaki
    (new[]{110,230,240,250}, new[]{110}),                      // Nassau
    (new[]{120}, null),                                        // start of game: already lowest -> unchanged
    (new[]{110,130}, null),
    (Array.Empty<int>(), null),                                // empty stays empty (original behaviour)
    (new[]{140,160}, null),                                    // would empty -> keep original (no exception path)
    (new[]{999,140}, new[]{999}),                              // unknown template is kept, not judged
};
foreach (var (input, expect) in cases)
{
    var got = Rules.Filter(input, T);
    Check(expect == null ? got == null : got != null && got.SequenceEqual(expect), $"{S(input.ToList())} -> {S(got)} expected {S(expect?.ToList())}");
}
// Order preserved, duplicates preserved.
Check(Rules.Filter(new[]{140,120,160,120}, T)!.SequenceEqual(new[]{120,120}), "order/duplicates");
// Every result contains only Technical 0 ships.
var all = new[]{120,140,160,180,190,200,260,210,110,220,130,270,280,310,290,320,300,230,240,250};
Check(Rules.Filter(all, T)!.All(id => tech[id] == 0), "all 20 -> only technical 0");
Check(Rules.MaxTechnical == 0, "MaxTechnical 0");

Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
