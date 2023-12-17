using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Resource
{
    public string Name { get; }

    public Resource(string name)
    {
        Name = name;
    }
}

class ResourceManager
{
    private readonly List<Resource> resources;
    private readonly SemaphoreSlim semaphore;

    public ResourceManager(IEnumerable<Resource> resources)
    {
        this.resources = new List<Resource>(resources);
        this.semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<bool> AcquireResourceAsync(Resource resource, int priority)
    {
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} is attempting to acquire {resource.Name} with priority {priority}");

        await semaphore.WaitAsync();

        try
        {
            await Task.Delay(100);

            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} acquired {resource.Name} with priority {priority}");
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }
}

class Program
{
    static async Task Main()
    {
        Resource cpu = new Resource("CPU");
        Resource ram = new Resource("RAM");
        Resource disk = new Resource("Disk");

        ResourceManager resourceManager = new ResourceManager(new[] { cpu, ram, disk });

        List<Task> tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            int priority = i + 1; 
            Resource requestedResource = i % 2 == 0 ? cpu : ram; 

            tasks.Add(Task.Run(async () =>
            {
                bool success = await resourceManager.AcquireResourceAsync(requestedResource, priority);
                if (success)
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} is using {requestedResource.Name} with priority {priority}");
                    await Task.Delay(500);
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} released {requestedResource.Name}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        Console.ReadLine(); 
    }
}
