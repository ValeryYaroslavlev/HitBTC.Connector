// Unit/Infrastructure/LockFreeMPSCQueueTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Infrastructure;

using Xunit;

namespace HitBTC.Connector.Tests.Unit.Infrastructure;

public class LockFreeMPSCQueueTests
{
    // Тестовый класс-обёртка для сообщений
    private sealed class Message
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    // Простой wrapper для int (так как очередь требует reference type)
    private sealed class IntMessage
    {
        public int Value { get; }
        public IntMessage(int value) => Value = value;
    }

    [Fact]
    public void Enqueue_Dequeue_SingleItem_ShouldWork()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<Message>();
        var message = new Message { Id = 1, Content = "Test" };

        // Act
        queue.Enqueue(message);
        var success = queue.TryDequeue(out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeSameAs(message);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ShouldReturnFalse()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<Message>();

        // Act
        var success = queue.TryDequeue(out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Enqueue_Dequeue_MultipleItems_ShouldMaintainOrder()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<Message>();
        var messages = Enumerable.Range(1, 100)
            .Select(i => new Message { Id = i, Content = $"Message {i}" })
            .ToList();

        // Act
        foreach (var msg in messages)
        {
            queue.Enqueue(msg);
        }

        var results = new List<Message>();
        while (queue.TryDequeue(out var msg))
        {
            results.Add(msg!);
        }

        // Assert
        results.Should().HaveCount(100);
        results.Select(m => m.Id).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Queue_MultiProducer_SingleConsumer_ShouldBeThreadSafe()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<IntMessage>();
        var producerCount = 10;
        var messagesPerProducer = 1000;
        var totalMessages = producerCount * messagesPerProducer;

        // Act - Multiple producers
        var producerTasks = Enumerable.Range(0, producerCount)
            .Select(producerId => Task.Run(() =>
            {
                for (int i = 0; i < messagesPerProducer; i++)
                {
                    queue.Enqueue(new IntMessage(producerId * messagesPerProducer + i));
                }
            }))
            .ToArray();

        Task.WaitAll(producerTasks);

        // Single consumer
        var results = new List<int>();
        while (queue.TryDequeue(out var msg))
        {
            results.Add(msg!.Value);
        }

        // Assert
        results.Should().HaveCount(totalMessages);
        results.Distinct().Should().HaveCount(totalMessages);
    }

    [Fact]
    public void Queue_ConcurrentProduceConsume_ShouldNotLoseMessages()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<IntMessage>();
        var cts = new CancellationTokenSource();
        var produced = new System.Collections.Concurrent.ConcurrentBag<int>();
        var consumed = new System.Collections.Concurrent.ConcurrentBag<int>();

        // Act
        // Producers
        var producerTasks = Enumerable.Range(0, 5)
            .Select(producerId => Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var id = producerId * 1000 + i;
                    queue.Enqueue(new IntMessage(id));
                    produced.Add(id);
                }
            }))
            .ToArray();

        // Consumer (started with delay to ensure some messages are queued)
        var consumerTask = Task.Run(async () =>
        {
            await Task.Delay(10);

            while (!cts.Token.IsCancellationRequested || queue.TryDequeue(out _))
            {
                if (queue.TryDequeue(out var msg))
                {
                    consumed.Add(msg!.Value);
                }
                else
                {
                    await Task.Delay(1);
                }
            }

            // Drain remaining
            while (queue.TryDequeue(out var msg))
            {
                consumed.Add(msg!.Value);
            }
        });

        Task.WaitAll(producerTasks);
        cts.CancelAfter(1000);
        consumerTask.Wait(2000);

        // Assert
        produced.Should().HaveCount(5000);
        consumed.Should().HaveCount(5000);
        consumed.OrderBy(x => x).Should().Equal(produced.OrderBy(x => x));
    }

    [Fact]
    public void Queue_LargeMessages_ShouldWork()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<Message>();
        var largeContent = new string('X', 10000);
        var count = 1000;

        // Act
        for (int i = 0; i < count; i++)
        {
            queue.Enqueue(new Message { Id = i, Content = largeContent });
        }

        var results = new List<Message>();
        while (queue.TryDequeue(out var msg))
        {
            results.Add(msg!);
        }

        // Assert
        results.Should().HaveCount(count);
        results.Should().OnlyContain(m => m.Content.Length == 10000);
    }

    [Fact]
    public void Queue_RapidEnqueueDequeue_ShouldWork()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<IntMessage>();
        var iterations = 10000;

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            queue.Enqueue(new IntMessage(i));

            if (queue.TryDequeue(out var msg))
            {
                msg!.Value.Should().Be(i);
            }
        }
    }
}