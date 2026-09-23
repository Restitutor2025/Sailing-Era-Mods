// Checks the edit definition against the real GameAssembly (SHA256, section mapping, bytes at the edit site).
// Usage: dotnet run -- <path to GameAssembly.dll>   (build.ps1 passes the installed one)
using System.Security.Cryptography;
using Restitutor.RebalanceBoarding;

int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }


// --- Grace (0.2.0) ---
var A = (IntPtr)0x1000; var B = (IntPtr)0x2000;
Check(Grace.ShouldRecord(1, true, false, 24), "record: last target leaves, progress 24");
Check(!Grace.ShouldRecord(2, false, false, 24), "no record: another target still boarded (original keeps progress)");
Check(!Grace.ShouldRecord(1, false, false, 24), "no record: leaving ship is not the target");
Check(!Grace.ShouldRecord(1, true, true, 24), "no record: melee lock (would re-trigger melee)");
Check(!Grace.ShouldRecord(1, true, false, 0), "no record: nothing to keep");
var r = new Grace.Record(A, 24, 10f);
Check(Grace.ShouldRestore(r, A, 12.9f), "restore: same ship after 2.9 s");
Check(Grace.ShouldRestore(r, A, 13f), "restore: same ship at exactly 3 s");
Check(!Grace.ShouldRestore(r, A, 13.01f), "no restore: after 3 s");
Check(!Grace.ShouldRestore(r, B, 11f), "no restore: different ship");
Check(!Grace.ShouldRestore(r, IntPtr.Zero, 11f), "no restore: no target");
Check(!Grace.ShouldRestore(r, A, 9f), "no restore: clock went backwards (scene reload)");
Check(Grace.Expired(r, 13.5f) && !Grace.Expired(r, 12f), "expiry");

// --- Gauge (0.3.0) ---
Check(Gauge.Percent(0, 40) == 0d, "gauge: 0 progress -> empty");
Check(Gauge.Percent(20, 40) == 50d, "gauge: half of the patched threshold");
Check(Gauge.Percent(40, 40) == 100d, "gauge: full at the threshold");
Check(Gauge.Percent(60, 40) == 100d, "gauge: clamped above the threshold");
Check(Gauge.Percent(20, 100) == 20d, "gauge: stock threshold when the byte patch is not applied");
Check(Gauge.Percent(20, 0) == 0d, "gauge: no divide by zero");
Check(Gauge.Percent(-5, 40) == 0d, "gauge: negative progress -> empty");
Check(Gauge.ShouldShow(1, false) && !Gauge.ShouldShow(0, false), "gauge: shown only with progress");
Check(!Gauge.ShouldShow(30, true), "gauge: hidden once melee starts");
Check(Gauge.FadeSeconds == 0.5f, "gauge: 0.5 s fade");

if (args.Length == 0) { Console.WriteLine($"PASS {pass} FAIL {fail} (GameAssembly checks skipped: no path)"); return fail == 0 ? 0 : 1; }
var ga = File.ReadAllBytes(args[0]);
Check(Convert.ToHexString(SHA256.HashData(ga)) == Patch.GameAssemblySha256, "GameAssembly SHA256");
long off = Patch.FileOffset(ga[..4096], Patch.MethodRva);
Check(off > 0, "RVA mapped to a section");
if (off <= 0) { Console.WriteLine($"PASS {pass} FAIL {fail}"); return 1; }
var body = ga.AsSpan((int)off, Patch.MethodLength).ToArray();
Check(Patch.BodyLooksRight(body), "edit site 83 F9 64 0F 8E at +0x1BF");
Check(Patch.ImmOffset == 0x1C1, "imm offset +0x1C1 (RVA 0x25CD8D1)");
Check(body[0] == 0x53 || body[0] == 0x40, "method starts with push rbx (not already hooked in file)");
var patched = Patch.Patched(body);
int diff = 0; for (int i = 0; i < body.Length; i++) if (body[i] != patched[i]) diff++;
Check(diff == 1 && patched[Patch.ImmOffset] == 40, "exactly one byte differs, new value 40");
Check(!Patch.BodyLooksRight(patched), "patched body is rejected as a baseline (no double patch)");
Check(Patch.NewThreshold < Patch.OriginalThreshold && Patch.NewThreshold <= 127, "imm8 stays a positive signed byte");
// Local evidence copy (not in the repo) must match the file when present.
string bin = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../evidence/CheckProgressFull.bin"));
if (File.Exists(bin)) Check(File.ReadAllBytes(bin).AsSpan().SequenceEqual(body), "evidence/CheckProgressFull.bin == file body");
Console.WriteLine($"PASS {pass} FAIL {fail}");
return fail == 0 ? 0 : 1;
