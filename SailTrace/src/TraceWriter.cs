using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Restitutor.SailTrace;

// Background JSONL writer. Game thread only enqueues; disk I/O and serialization run on the worker.
internal sealed class TraceWriter : IDisposable
{
    private readonly BlockingCollection<object> queue = new(16384);
    private readonly Thread worker;
    private readonly StreamWriter output;
    private long dropped;
    private volatile string? failure;
    public string PathName { get; }
    public string? Failure => failure;
    public long Dropped => Interlocked.Read(ref dropped);
    public static double Now => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency);
    public static readonly double Origin = Now;
    public static double Ms => Now - Origin;

    public TraceWriter(string directory)
    {
        Directory.CreateDirectory(directory);
        PathName = Path.Combine(directory, $"SailTrace-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.jsonl");
        output = new StreamWriter(new FileStream(PathName, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
        worker = new Thread(Run) { IsBackground = true, Name = "SailTrace writer" };
        worker.Start();
    }

    public void Add(object row)
    {
        if (failure != null || queue.IsAddingCompleted) return;
        try { if (!queue.TryAdd(row)) Interlocked.Increment(ref dropped); }
        catch (InvalidOperationException) { }
    }

    private void Run()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            double nextSample = 0, nextFlush = 0;
            while (!queue.IsCompleted)
            {
                if (queue.TryTake(out var row, 250)) output.WriteLine(JsonSerializer.Serialize(row));
                double now = Now;
                if (now >= nextSample)
                {
                    process.Refresh();
                    output.WriteLine(JsonSerializer.Serialize(new
                    {
                        k = "proc", ms = Math.Round(now - Origin, 1),
                        workingSetMB = process.WorkingSet64 / 1048576, privateMB = process.PrivateMemorySize64 / 1048576,
                        cpuMs = Math.Round(process.TotalProcessorTime.TotalMilliseconds), dropped = Dropped
                    }));
                    nextSample = now + 5000;
                }
                if (now >= nextFlush)
                {
                    output.Flush(); nextFlush = now + 1000;
                    if (output.BaseStream.Position >= 128L * 1024 * 1024)
                        throw new IOException("128 MiB session log limit reached; recording stopped.");
                }
            }
        }
        catch (Exception ex) { failure = ex.GetType().Name + ": " + ex.Message; }
        finally { try { output.Flush(); output.Dispose(); } catch { } }
    }

    public void Dispose()
    {
        queue.CompleteAdding();
        worker.Join(3000);
    }
}
