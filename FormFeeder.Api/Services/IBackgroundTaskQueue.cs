using System.Collections.Concurrent;

namespace FormFeeder.Api.Services;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<CancellationToken, ValueTask>> workItems = new();
    private readonly SemaphoreSlim signal = new(0);

    public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        workItems.Enqueue(workItem);
        signal.Release();

        return ValueTask.CompletedTask;
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
        workItems.TryDequeue(out var workItem);

        return workItem!;
    }
}
