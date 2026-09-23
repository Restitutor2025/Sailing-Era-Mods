using Restitutor.RebalanceSaveSlots;

int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }

Check(Rules.Total == 101, "total 101");
Check(Rules.AutoCount == 2 && Rules.ManualCount == 99, "2 auto + 99 manual");
Check(Rules.Missing(10).SequenceEqual(Enumerable.Range(10, 91)), "pad 10 -> 101");
Check(!Rules.Missing(101).Any() && !Rules.Missing(150).Any(), "full list: nothing to add");
Check(Rules.Missing(0).Count() == 101, "empty list -> 101");
// Game loads 10 of a 101-entry file -> restore 10..100.
Check(Rules.ToRestore(101, 10).SequenceEqual(Enumerable.Range(10, 91)), "restore 10..100");
// Old file (10 entries) -> nothing to restore, padding does the rest.
Check(!Rules.ToRestore(10, 10).Any(), "old 10-entry file");
// File longer than Total (should not happen) -> capped at Total.
Check(Rules.ToRestore(150, 10).Last() == 100, "cap at 100");
// Loader copied fewer than 10 (short file) -> restore nothing beyond the file.
Check(!Rules.ToRestore(5, 5).Any(), "short file");
Check(Rules.IsAuto(0) && Rules.IsAuto(1) && !Rules.IsAuto(2) && !Rules.IsAuto(100), "auto = 0,1");
Check(!Rules.ShowInstantly(0) && !Rules.ShowInstantly(9) && Rules.ShowInstantly(10) && Rules.ShowInstantly(100) && !Rules.ShowInstantly(101), "instant rows 10..100");
Check(Rules.ReselectAfterGrow(15, 101) == 15 && Rules.ReselectAfterGrow(3, 101) == -1 && Rules.ReselectAfterGrow(101, 101) == -1 && Rules.ReselectAfterGrow(-1, 101) == -1, "reselect only 10..count-1");
Check(Rules.Page == 5, "page 5");
Check(Rules.Paged(0, 1, 101) == 5 && Rules.Paged(5, -1, 101) == 0, "page down/up by 5");
Check(Rules.Paged(98, 1, 101) == 100 && Rules.Paged(100, 1, 101) == -1, "clamp at the last row");
Check(Rules.Paged(2, -1, 101) == 0 && Rules.Paged(0, -1, 101) == -1, "clamp at the first row");
Check(Rules.Paged(-1, 1, 101) == 5 && Rules.Paged(0, 1, 0) == -1, "no selection / empty list");
Check(Rules.FocusRow(37, 4, 101) == 37, "focus: slot used in this run");
Check(Rules.FocusRow(-1, 4, 101) == 4, "focus: latest save");
Check(Rules.FocusRow(-1, -1, 101) == 0 && Rules.FocusRow(150, 200, 101) == 0, "focus: fallback to row 0");

Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
