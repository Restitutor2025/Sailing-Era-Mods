using Restitutor.TextSpeed;
var dir=Path.Combine(AppContext.BaseDirectory,"cases",Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(dir);
var p=Path.Combine(dir,"textspeed.json");
int checks=0;
void Check(bool condition,string name) { if(!condition)throw new Exception(name); Console.WriteLine("PASS "+name); checks++; }
var s=new ModeStore(p);
Check(s.Ready && s.Current==TextMode.Slow,"new installation uses slow");
var original=File.ReadAllBytes(p);var stamp=File.GetLastWriteTimeUtc(p);
for(int i=0;i<100;i++)CheckNoWrite();
void CheckNoWrite(){if(!s.SelectFromOptions(TextMode.Slow))throw new Exception("same mode");}
Check(File.GetLastWriteTimeUtc(p)==stamp && original.SequenceEqual(File.ReadAllBytes(p)),"redraw/same-mode selection does not rewrite settings");
Check(s.SelectFromOptions(TextMode.Instant) && s.Current==TextMode.Instant,"explicit choice commits instant");
Check(new ModeStore(p).Current==TextMode.Instant,"restart keeps instant");
Check(File.Exists(p+".bak"),"atomic replace keeps backup");
for(int i=0;i<40;i++){var mode=i%2==0?TextMode.Slow:TextMode.Instant;Check(s.SelectFromOptions(mode) && new ModeStore(p).Current==mode,"roundtrip "+i);}
File.WriteAllText(p,"corrupt");
Check(!s.SelectFromOptions(TextMode.Slow) && s.Current==TextMode.Instant,"external edit cannot change in-memory choice or be silently overwritten");
var recovered=new ModeStore(p);
Check(!recovered.Ready && recovered.RecoveredBackup && File.ReadAllText(p)=="corrupt","corruption preserves damaged file and blocks changes");
File.WriteAllText(p+".bak","also corrupt");
Check(!new ModeStore(p).Ready,"two corrupt copies never silently reset persisted preference");
Console.WriteLine($"{checks} checks passed; evidence directory: {dir}");
