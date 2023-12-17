using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class LamportClock
{
    private int clockValue = 0;
    private readonly object clockLock = new object();

    public int Value
    {
        get
        {
            lock (clockLock)
            {
                return clockValue;
            }
        }
    }

    public int Tick()
    {
        lock (clockLock)
        {
            return ++clockValue;
        }
    }

    public void Update(int receivedClock)
    {
        lock (clockLock)
        {
            clockValue = Math.Max(clockValue, receivedClock) + 1;
        }
    }
}

class Event
{
    public string Name { get; }
    public LamportClock Timestamp { get; }

    public Event(string name, LamportClock timestamp)
    {
        Name = name;
        Timestamp = timestamp;
    }
}

class DistributedSystemNode
{
    private readonly string nodeId;
    private readonly List<DistributedSystemNode> nodes;
    private readonly LamportClock lamportClock;
    private readonly ConcurrentQueue<Event> eventQueue;

    public event EventHandler<Event> EventOccurred;

    public DistributedSystemNode(string nodeId, List<DistributedSystemNode> nodes)
    {
        this.nodeId = nodeId;
        this.nodes = nodes;
        this.lamportClock = new LamportClock();
        this.eventQueue = new ConcurrentQueue<Event>();

        Task.Run(() => ProcessEventsAsync());
    }

    public void SendEvent(string eventName)
    {
        var timestamp = lamportClock.Tick();
        var newEvent = new Event(eventName, new LamportClock { Value = timestamp });

        Thread.Sleep(100);

        foreach (var node in nodes)
        {
            node.ReceiveEvent(newEvent);
        }

        ProcessEvent(newEvent);
    }

    public void ReceiveEvent(Event receivedEvent)
    {
        lamportClock.Update(receivedEvent.Timestamp.Value);

        eventQueue.Enqueue(receivedEvent);
    }

    private async Task ProcessEventsAsync()
    {
        while (true)
        {
            if (eventQueue.TryDequeue(out var nextEvent))
            {
                await Task.Delay(100);

                ProcessEvent(nextEvent);
            }

            await Task.Delay(10);
        }
    }

    private void ProcessEvent(Event receivedEvent)
    {
        Console.WriteLine($"Node {nodeId} processed event: {receivedEvent.Name} at timestamp {receivedEvent.Timestamp.Value}");
        EventOccurred?.Invoke(this, receivedEvent);
    }
}

class Program
{
    static async Task Main()
    {
        var node1 = new DistributedSystemNode("Node1", new List<DistributedSystemNode>());
        var node2 = new DistributedSystemNode("Node2", new List<DistributedSystemNode> { node1 });
        var node3 = new DistributedSystemNode("Node3", new List<DistributedSystemNode> { node1, node2 });

        node1.EventOccurred += (sender, e) => Console.WriteLine($"Subscriber received event: {e.Name}");

        node1.SendEvent("EventA");
        node2.SendEvent("EventB");
        node3.SendEvent("EventC");

        await Task.Delay(500);

        Console.ReadLine(); 
    }
}
