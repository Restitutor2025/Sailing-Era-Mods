using Restitutor.CTRLInstant;

int checks = 0;
void Check(bool result, string message)
{
    if (!result) throw new Exception(message);
    checks++;
}
var pauses = new HashSet<int> { 17, 23 }; // CTRL and a separate menu.
var lease = new PauseLease();
lease.Take(17, (nint)100);
int callbacks = 0;
void Resume(int token) { callbacks++; pauses.Remove(token); }
lease.Release((nint)100, pauses.Contains, Resume);
Check(!pauses.Contains(17) && pauses.Contains(23), "CTRL release must retain menu pause");
Check(!lease.Active, "release clears ownership");
lease.Release((nint)100, pauses.Contains, Resume);
Check(callbacks == 1, "key-up and focus-loss cleanup must not double resume");

lease.Take(17, (nint)100);
pauses.Add(17);
lease.Release((nint)200, pauses.Contains, Resume);
Check(pauses.Contains(17) && callbacks == 1, "a reused numeric ID in another session must survive");
Check(!lease.Active, "old session must be forgotten");

lease.Take(17, (nint)100);
pauses.Remove(17);
lease.Release((nint)100, pauses.Contains, Resume);
Check(callbacks == 1, "native reset/removal must not emit extra resume");

lease.Take(17, (nint)100);
pauses.Add(17);
lease.Release((nint)100, pauses.Contains, token => {
    Check(!lease.Active, "resume notification must see detached lease");
    lease.Release((nint)100, pauses.Contains, Resume);
    Resume(token);
});
Check(callbacks == 2 && pauses.SetEquals(new[] { 23 }), "reentrant cleanup retains unrelated pause");

lease.Take(17, (nint)100);
lease.Forget(); // Native ForceReset owns clearing its list.
lease.Release((nint)100, _ => true, Resume);
Check(callbacks == 2, "ForceReset invalidates local ownership");

for (int i = 0; i < 50; i++)
{
    int token = 1000 + i;
    pauses.Add(token); lease.Take(token, (nint)100);
    lease.Release((nint)100, pauses.Contains, Resume);
}
Check(pauses.SetEquals(new[] { 23 }), "repeated quick taps must never accumulate pauses");
foreach (var invalid in new[] { 0, -1 })
{
    bool rejected = false;
    try { lease.Take(invalid, (nint)100); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected && !lease.Active, "invalid pause ID rejected");
}
lease.Take(99, (nint)100);
bool duplicateRejected = false;
try { lease.Take(100, (nint)100); } catch (InvalidOperationException) { duplicateRejected = true; }
Check(duplicateRejected && lease.Token == 99, "duplicate start cannot overwrite an outstanding pause");
Console.WriteLine($"PASS {checks} ownership/lifecycle checks (including 50 quick-tap cycles). No game loaded.");
