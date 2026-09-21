namespace Restitutor.Core;

/// <summary>Registrations waiting for one moment. Each runs once, in order; one failure never stops the rest.</summary>
public sealed class DeferredQueue
{
    private readonly List<(string Owner, Action Action)> items = new();

    public int Count => items.Count;

    public void Add(string owner, Action action)
    {
        if (string.IsNullOrEmpty(owner)) throw new ArgumentException("owner is required", nameof(owner));
        items.Add((owner, action ?? throw new ArgumentNullException(nameof(action))));
    }

    /// <summary>Runs and removes every queued item. Items added while running are left for the next call.</summary>
    public void RunAll(Action<string, Exception> onError)
    {
        var batch = items.ToArray();
        items.Clear();
        foreach (var (owner, action) in batch) RunIsolated(owner, action, onError);
    }

    public static void RunIsolated(string owner, Action action, Action<string, Exception> onError)
    {
        try { action(); }
        catch (Exception ex) { try { onError(owner, ex); } catch { } }
    }
}
