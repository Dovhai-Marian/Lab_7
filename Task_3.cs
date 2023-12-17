using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class Operation
{
    public string ThreadId { get; }
    public DateTime Timestamp { get; }

    public Operation(string threadId)
    {
        ThreadId = threadId;
        Timestamp = DateTime.Now;
    }
}

class ConflictResolutionSystem
{
    private readonly ConcurrentDictionary<string, Operation> operationLog;

    public ConflictResolutionSystem()
    {
        operationLog = new ConcurrentDictionary<string, Operation>();
    }

    public void AddOperation(string resourceId)
    {
        string threadId = Thread.CurrentThread.ManagedThreadId.ToString();
        Operation operation = new Operation(threadId);

        if (!operationLog.TryAdd(resourceId, operation))
        {
            ResolveConflict(resourceId, operation);
        }
        else
        {
            Console.WriteLine($"Operation added to the log: Thread {threadId} added an operation for resource {resourceId} at {operation.Timestamp}");
        }
    }

    private void ResolveConflict(string resourceId, Operation currentOperation)
    {
        Console.WriteLine($"Conflict detected: Thread {currentOperation.ThreadId} and another thread are trying to modify resource {resourceId} simultaneously at {currentOperation.Timestamp}");
        
        AddOperation(resourceId);
    }
}

class Program
{
    static async Task Main()
    {
        ConflictResolutionSystem conflictResolutionSystem = new ConflictResolutionSystem();

        List<Task> tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            string resourceId = "Resource1";

            tasks.Add(Task.Run(() =>
            {
                conflictResolutionSystem.AddOperation(resourceId);
            }));

            await Task.Delay(50);
        }

        await Task.WhenAll(tasks);

        Console.ReadLine();
    }
}
