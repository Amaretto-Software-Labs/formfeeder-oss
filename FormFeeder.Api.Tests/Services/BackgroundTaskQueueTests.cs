using System.Collections.Concurrent;

using FormFeeder.Api.Services;

namespace FormFeeder.Api.Tests.Services;

public class BackgroundTaskQueueTests
{
    private readonly BackgroundTaskQueue queue;

    public BackgroundTaskQueueTests()
    {
        queue = new BackgroundTaskQueue();
    }

    public class QueueBackgroundWorkItemAsync : BackgroundTaskQueueTests
    {
        [Fact]
        public async Task QueueBackgroundWorkItemAsync_WithValidWorkItem_ShouldQueue()
        {
            // Arrange
            var executed = false;
            Func<CancellationToken, ValueTask> workItem = _ =>
            {
                executed = true;
                return ValueTask.CompletedTask;
            };

            // Act
            await queue.QueueBackgroundWorkItemAsync(workItem);
            var dequeuedItem = await queue.DequeueAsync(CancellationToken.None);
            await dequeuedItem(CancellationToken.None);

            // Assert
            executed.Should().BeTrue();
        }

        [Fact]
        public async Task QueueBackgroundWorkItemAsync_WithNullWorkItem_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = async () => await queue.QueueBackgroundWorkItemAsync(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task QueueBackgroundWorkItemAsync_ShouldReturnCompletedTask()
        {
            // Arrange
            Func<CancellationToken, ValueTask> workItem = _ => ValueTask.CompletedTask;

            // Act
            var task = queue.QueueBackgroundWorkItemAsync(workItem);

            // Assert
            task.IsCompleted.Should().BeTrue();
            await task; // Should not throw or hang
        }
    }

    public class DequeueAsync : BackgroundTaskQueueTests
    {
        [Fact]
        public async Task DequeueAsync_WithQueuedItem_ShouldReturnWorkItem()
        {
            // Arrange
            var executed = false;
            Func<CancellationToken, ValueTask> workItem = _ =>
            {
                executed = true;
                return ValueTask.CompletedTask;
            };

            await queue.QueueBackgroundWorkItemAsync(workItem);

            // Act
            var dequeuedItem = await queue.DequeueAsync(CancellationToken.None);
            await dequeuedItem(CancellationToken.None);

            // Assert
            executed.Should().BeTrue();
        }

        [Fact]
        public async Task DequeueAsync_WithMultipleItems_ShouldReturnFIFO()
        {
            // Arrange
            var executionOrder = new List<int>();

            Func<CancellationToken, ValueTask> workItem1 = _ =>
            {
                executionOrder.Add(1);
                return ValueTask.CompletedTask;
            };

            Func<CancellationToken, ValueTask> workItem2 = _ =>
            {
                executionOrder.Add(2);
                return ValueTask.CompletedTask;
            };

            await queue.QueueBackgroundWorkItemAsync(workItem1);
            await queue.QueueBackgroundWorkItemAsync(workItem2);

            // Act
            var firstItem = await queue.DequeueAsync(CancellationToken.None);
            var secondItem = await queue.DequeueAsync(CancellationToken.None);

            await firstItem(CancellationToken.None);
            await secondItem(CancellationToken.None);

            // Assert
            executionOrder.Should().Equal(1, 2);
        }

        [Fact]
        public async Task DequeueAsync_WithCancellation_ShouldThrowOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => queue.DequeueAsync(cts.Token).AsTask())
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DequeueAsync_WithDelayedCancellation_ShouldCancel()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            await FluentActions.Invoking(() => queue.DequeueAsync(cts.Token).AsTask())
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DequeueAsync_WhenWaitingAndItemIsQueued_ShouldReturnImmediately()
        {
            // Arrange
            var executed = false;
            Func<CancellationToken, ValueTask> workItem = _ =>
            {
                executed = true;
                return ValueTask.CompletedTask;
            };

            // Start dequeue operation (will wait)
            var dequeueTask = queue.DequeueAsync(CancellationToken.None);

            // Queue an item after a short delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                await queue.QueueBackgroundWorkItemAsync(workItem);
            });

            // Act
            var dequeuedItem = await dequeueTask;
            await dequeuedItem(CancellationToken.None);

            // Assert
            executed.Should().BeTrue();
        }
    }

    public class ConcurrencyTests : BackgroundTaskQueueTests
    {
        [Fact]
        public async Task QueueAndDequeue_WithConcurrentOperations_ShouldWorkCorrectly()
        {
            // Arrange
            const int itemCount = 100;
            var executedItems = new List<int>();
            var lockObject = new object();

            var queueTasks = new List<Task>();
            var dequeueTasks = new List<Task>();

            // Queue items concurrently
            for (int i = 0; i < itemCount; i++)
            {
                var index = i; // Capture variable
                queueTasks.Add(Task.Run(async () =>
                {
                    Func<CancellationToken, ValueTask> workItem = _ =>
                    {
                        lock (lockObject)
                        {
                            executedItems.Add(index);
                        }

                        return ValueTask.CompletedTask;
                    };
                    await queue.QueueBackgroundWorkItemAsync(workItem);
                }));
            }

            // Dequeue items concurrently
            for (int i = 0; i < itemCount; i++)
            {
                dequeueTasks.Add(Task.Run(async () =>
                {
                    var workItem = await queue.DequeueAsync(CancellationToken.None);
                    await workItem(CancellationToken.None);
                }));
            }

            // Act
            await Task.WhenAll(queueTasks);
            await Task.WhenAll(dequeueTasks);

            // Assert
            executedItems.Should().HaveCount(itemCount);
            executedItems.Should().OnlyHaveUniqueItems();
            executedItems.Should().BeEquivalentTo(Enumerable.Range(0, itemCount));
        }

        [Fact]
        public async Task MultipleConsumers_ShouldProcessItemsConcurrently()
        {
            // Arrange
            const int itemCount = 50;
            const int consumerCount = 5;
            var processedItems = new ConcurrentBag<int>();

            // Queue items
            for (int i = 0; i < itemCount; i++)
            {
                var index = i;
                Func<CancellationToken, ValueTask> workItem = async _ =>
                {
                    await Task.Delay(10); // Simulate work
                    processedItems.Add(index);
                };
                await queue.QueueBackgroundWorkItemAsync(workItem);
            }

            // Create multiple consumers
            var consumerTasks = new List<Task>();
            for (int i = 0; i < consumerCount; i++)
            {
                consumerTasks.Add(Task.Run(async () =>
                {
                    var itemsProcessed = 0;
                    while (itemsProcessed < itemCount / consumerCount)
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            var workItem = await queue.DequeueAsync(cts.Token);
                            await workItem(CancellationToken.None);
                            itemsProcessed++;
                        }
                        catch (OperationCanceledException)
                        {
                            break; // No more items to process
                        }
                    }
                }));
            }

            // Act
            await Task.WhenAll(consumerTasks);

            // Assert
            processedItems.Should().HaveCount(itemCount);
            processedItems.Should().OnlyHaveUniqueItems();
        }
    }
}
