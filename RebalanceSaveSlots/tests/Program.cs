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
Check(Rules.Page == 5 && Rules.PageCount(101) == 21 && Rules.PageCount(10) == 2 && Rules.PageCount(0) == 0, "21 pages of 5");
Check(Rules.PageOf(0) == 0 && Rules.PageOf(4) == 0 && Rules.PageOf(5) == 1 && Rules.PageOf(100) == 20 && Rules.PageOf(-1) == 0, "page of row");
Check(Rules.PageStart(0, 101) == 0 && Rules.PageStart(20, 101) == 100 && Rules.PageStart(99, 101) == 100 && Rules.PageStart(-3, 101) == 0, "page start");
Check(Rules.PageStep(7, 1, 101) == 10 && Rules.PageStep(7, -1, 101) == 0 && Rules.PageStep(5, -1, 101) == 0, "E/Q go to next/previous page start");
Check(Rules.PageStep(100, 1, 101) == -1 && Rules.PageStep(3, -1, 101) == -1 && Rules.PageStep(96, 1, 101) == 100, "stop at first/last page");
Check(Rules.PageStep(-1, 1, 101) == 5 && Rules.PageStep(0, 1, 0) == -1, "no selection / empty list");
Check(Rules.FocusRow(37, 4, 101) == 37, "focus: slot used in this run");
Check(Rules.FocusRow(-1, 4, 101) == 4, "focus: latest save");
Check(Rules.FocusRow(-1, -1, 101) == 0 && Rules.FocusRow(150, 200, 101) == 0, "focus: fallback to row 0");
var rows = new List<(bool, long)> { (true, 900), (true, 800), (true, 100), (false, 0), (true, 500), (true, 300) };
Check(Rules.LatestManual(rows) == 4, "latest manual skips auto 0,1 and empty rows");
Check(Rules.LatestManual(new List<(bool, long)> { (true, 9), (true, 8), (false, 0) }) == -1, "no manual save -> -1");

Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
