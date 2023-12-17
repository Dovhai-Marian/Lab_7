using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class DistributedSystemNode
{
    private readonly string nodeId;
    private readonly ConcurrentQueue<string> messageQueue;
    private readonly CancellationTokenSource cancellationTokenSource;

    public event EventHandler<string> MessageReceived;
    public event EventHandler<bool> NodeStatusChanged;

    public DistributedSystemNode(string nodeId)
    {
        this.nodeId = nodeId;
        this.messageQueue = new ConcurrentQueue<string>();
        this.cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => ProcessMessagesAsync(cancellationTokenSource.Token));
    }

    public void SendMessage(string message)
    {
        messageQueue.Enqueue(message);
    }

    public void StopNode()
    {
        cancellationTokenSource.Cancel();
        NodeStatusChanged?.Invoke(this, false);
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        NodeStatusChanged?.Invoke(this, true);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (messageQueue.TryDequeue(out var message))
            {
                await Task.Delay(100);

                MessageReceived?.Invoke(this, $"{nodeId} received: {message}");
            }

            await Task.Delay(10);
        }
    }
}

class Program
{
    static async Task Main()
    {
        DistributedSystemNode node1 = new DistributedSystemNode("Node1");
        DistributedSystemNode node2 = new DistributedSystemNode("Node2");

        node1.MessageReceived += (sender, message) => Console.WriteLine(message);
        node2.MessageReceived += (sender, message) => Console.WriteLine(message);

        node1.SendMessage("Hello from Node1");
        node2.SendMessage("Hello from Node2");

        await Task.Delay(500);

        node1.StopNode();

        Console.ReadLine();
    }
}
