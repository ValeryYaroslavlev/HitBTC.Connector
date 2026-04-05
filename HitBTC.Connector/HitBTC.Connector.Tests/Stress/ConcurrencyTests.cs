// Stress/ConcurrencyTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Infrastructure;

using Xunit;

namespace HitBTC.Connector.Tests.Stress;

[Trait("Category", "Stress")]
public class ConcurrencyTests
{
    // Wrapper для int, так как LockFreeMPSCQueue требует reference type
    private sealed class BoxedInt
    {
        public int Value { get; }
        public BoxedInt(int value) => Value = value;
    }

    [Fact]
    public async Task LockFreeQueue_HighConcurrency_ShouldNotLoseMessages()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<BoxedInt>();
        var producerCount = Environment.ProcessorCount;
        var messagesPerProducer = 100_000;
        var totalExpected = producerCount * messagesPerProducer;

        var produced = new System.Collections.Concurrent.ConcurrentBag<int>();
        var consumed = new System.Collections.Concurrent.ConcurrentBag<int>();

        // Act - Producers
        var producerTasks = Enumerable.Range(0, producerCount)
            .Select(producerId => Task.Run(() =>
            {
                for (int i = 0; i < messagesPerProducer; i++)
                {
                    var value = producerId * messagesPerProducer + i;
                    queue.Enqueue(new BoxedInt(value));
                    produced.Add(value);
                }
            }))
            .ToArray();

        // Consumer
        var cts = new CancellationTokenSource();
        var consumerTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                while (queue.TryDequeue(out var item))
                {
                    consumed.Add(item!.Value);
                }
                await Task.Yield();
            }

            // Final drain
            while (queue.TryDequeue(out var item))
            {
                consumed.Add(item!.Value);
            }
        });

        await Task.WhenAll(producerTasks);
        cts.CancelAfter(5000);
        await consumerTask;

        // Assert
        produced.Should().HaveCount(totalExpected);
        consumed.Should().HaveCount(totalExpected);
        consumed.OrderBy(x => x).Should().Equal(produced.OrderBy(x => x));
    }

    [Fact]
    public void ObjectPool_HighConcurrency_ShouldNotCorrupt()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestPoolObject>(256);
        var operations = 1_000_000;
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, operations, i =>
        {
            try
            {
                var obj = pool.Rent();
                obj.Value = i;
                obj.Data = $"Data_{i}";

                // Validate
                if (obj.Value != i)
                {
                    throw new Exception($"Value corruption: expected {i}, got {obj.Value}");
                }

                if (i % 2 == 0)
                {
                    pool.Return(obj);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void ValueStringBuilder_ParallelUsage_ShouldWork()
    {
        // Arrange
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        var expected = new System.Collections.Concurrent.ConcurrentBag<string>();
        var iterations = 10000;

        // Act - используем Parallel.For вместо Parallel.ForEachAsync с лямбдой
        // ValueStringBuilder - ref struct, нельзя использовать в лямбдах напрямую
        // Поэтому создаём его внутри обычного метода
        Parallel.For(0, iterations, i =>
        {
            var result = BuildString(i);
            results.Add(result);
            expected.Add($"Item_{i}_End");
        });

        // Assert
        results.Should().HaveCount(iterations);
        results.OrderBy(x => x).Should().Equal(expected.OrderBy(x => x));
    }

    // Отдельный метод для работы с ValueStringBuilder
    // (ref struct нельзя использовать в лямбдах, но можно в обычных методах)
    private static string BuildString(int i)
    {
        Span<char> buffer = stackalloc char[100];
        var sb = new ValueStringBuilder(buffer);

        sb.Append("Item_");
        sb.Append(i.ToString());
        sb.Append("_End");

        return sb.ToString();
    }

    [Fact]
    public void ValueStringBuilder_SequentialPerformance_ShouldBeEfficient()
    {
        // Arrange
        var iterations = 100_000;
        var results = new string[iterations];

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            results[i] = BuildUrlPath(i);
        }

        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        opsPerSecond.Should().BeGreaterThan(100_000, "ValueStringBuilder should be fast");

        // Verify correctness
        results[0].Should().Be("public/orderbook/BTCUSDT?depth=0");
        results[999].Should().Be("public/orderbook/BTCUSDT?depth=999");
    }

    private static string BuildUrlPath(int depth)
    {
        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);

        sb.Append("public/orderbook/BTCUSDT?depth=");
        sb.Append(depth.ToString());

        return sb.ToString();
    }

    private sealed class TestPoolObject
    {
        public int Value { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}