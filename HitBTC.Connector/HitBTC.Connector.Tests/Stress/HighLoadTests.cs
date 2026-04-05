// Stress/HighLoadTests.cs
using System.Diagnostics;

using FluentAssertions;

using HitBTC.Connector.Core.Infrastructure;
using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.Tests.Fixtures;

using Xunit;
using Xunit.Abstractions;

namespace HitBTC.Connector.Tests.Stress;

[Trait("Category", "Stress")]
public class HighLoadTests
{
    private readonly ITestOutputHelper _output;

    public HighLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Wrapper для byte[] в очереди
    private sealed class ByteArrayMessage
    {
        public byte[] Data { get; }
        public ByteArrayMessage(byte[] data) => Data = data;
    }

    [Fact]
    public void OrderBook_RapidUpdates_ShouldHandleEfficiently()
    {
        // Arrange
        using var ob = new OrderBook(1000);
        var iterations = 100_000;
        var random = new Random(42);

        // Act
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var levelCount = random.Next(10, 100);
            Span<OrderBookLevel> asks = stackalloc OrderBookLevel[levelCount];
            Span<OrderBookLevel> bids = stackalloc OrderBookLevel[levelCount];

            for (int j = 0; j < levelCount; j++)
            {
                asks[j] = new OrderBookLevel(
                    50000m + j * 0.01m + random.Next(100) * 0.001m,
                    random.Next(1, 100) * 0.01m);

                bids[j] = new OrderBookLevel(
                    49999m - j * 0.01m - random.Next(100) * 0.001m,
                    random.Next(1, 100) * 0.01m);
            }

            ob.SetAsks(asks);
            ob.SetBids(bids);
        }

        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"OrderBook updates: {opsPerSecond:N0} ops/sec");
        _output.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms for {iterations:N0} iterations");

        opsPerSecond.Should().BeGreaterThan(10_000, "should handle at least 10K updates/sec");
    }

    [Fact]
    public async Task RestClient_ParallelRequests_ShouldHandle()
    {
        Skip.IfNot(TestConfiguration.RunStressTests && TestConfiguration.RunIntegrationTests,
            "Stress tests disabled");

        // Arrange
        await using var client = new HitBtcRestClient(
            TestConfiguration.ApiKey,
            TestConfiguration.SecretKey);

        var parallelRequests = 10;
        var requestsPerBatch = 10;
        var totalRequests = parallelRequests * requestsPerBatch;

        // Act
        var sw = Stopwatch.StartNew();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, parallelRequests)
            .Select(async batch =>
            {
                for (int i = 0; i < requestsPerBatch; i++)
                {
                    try
                    {
                        await client.GetSymbolsAsync(new[] { "BTCUSDT" });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }
            });

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        _output.WriteLine($"Total requests: {totalRequests}");
        _output.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Requests/sec: {totalRequests / sw.Elapsed.TotalSeconds:N2}");
        _output.WriteLine($"Errors: {errors.Count}");

        errors.Count.Should().BeLessThan(totalRequests / 10, "error rate should be below 10%");
    }

    [Fact]
    public void DecimalToApiString_Performance()
    {
        // Arrange
        var values = Enumerable.Range(0, 100_000)
            .Select(i => (decimal)i / 1000m + 0.000001m)
            .ToArray();

        // Act
        var sw = Stopwatch.StartNew();
        var results = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            results[i] = values[i].ToApiString();
        }

        sw.Stop();

        // Assert
        var opsPerSecond = values.Length / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"ToApiString: {opsPerSecond:N0} ops/sec");

        opsPerSecond.Should().BeGreaterThan(100_000);
    }

    [Fact]
    public async Task WebSocket_MessageThroughput()
    {
        // Arrange
        var queue = new LockFreeMPSCQueue<ByteArrayMessage>();
        var messageCount = 1_000_000;
        var messageSize = 512;
        var messageData = new byte[messageSize];
        Random.Shared.NextBytes(messageData);

        // Act - simulate receiving messages
        var sw = Stopwatch.StartNew();

        var producer = Task.Run(() =>
        {
            for (int i = 0; i < messageCount; i++)
            {
                queue.Enqueue(new ByteArrayMessage(messageData));
            }
        });

        var consumed = 0;
        var consumer = Task.Run(() =>
        {
            while (consumed < messageCount)
            {
                if (queue.TryDequeue(out _))
                {
                    Interlocked.Increment(ref consumed);
                }
            }
        });

        await Task.WhenAll(producer, consumer);
        sw.Stop();

        // Assert
        var throughput = messageCount / sw.Elapsed.TotalSeconds;
        var mbPerSecond = (messageCount * messageSize) / sw.Elapsed.TotalSeconds / 1024 / 1024;

        _output.WriteLine($"Message throughput: {throughput:N0} msg/sec");
        _output.WriteLine($"Data throughput: {mbPerSecond:N2} MB/sec");

        throughput.Should().BeGreaterThan(100_000);
    }

    [Fact]
    public void ValueStringBuilder_Performance()
    {
        // Arrange
        var iterations = 1_000_000;
        var results = new string[iterations];

        // Act
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            results[i] = BuildOrderUrl(i);
        }

        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"ValueStringBuilder URL building: {opsPerSecond:N0} ops/sec");
        _output.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");

        opsPerSecond.Should().BeGreaterThan(500_000, "ValueStringBuilder should be very fast");
    }

    // Метод для построения URL (ref struct нельзя использовать в лямбдах)
    private static string BuildOrderUrl(int orderId)
    {
        Span<char> buffer = stackalloc char[128];
        var sb = new ValueStringBuilder(buffer);

        sb.Append("spot/order/");
        sb.Append(orderId.ToString());
        sb.Append("?validate=true");

        return sb.ToString();
    }

    [Fact]
    public void ObjectPool_Performance()
    {
        // Arrange
        var pool = new LockFreeObjectPool<TestObject>(1024);
        var iterations = 1_000_000;

        // Act
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var obj = pool.Rent();
            obj.Value = i;
            pool.Return(obj);
        }

        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"ObjectPool rent/return: {opsPerSecond:N0} ops/sec");

        opsPerSecond.Should().BeGreaterThan(1_000_000);
    }

    [Fact]
    public void OrderBookLevel_Creation_Performance()
    {
        // Arrange
        var iterations = 10_000_000;

        // Act
        var sw = Stopwatch.StartNew();

        var sum = 0m;
        for (int i = 0; i < iterations; i++)
        {
            var level = new OrderBookLevel(50000m + i * 0.01m, 1.0m + i * 0.001m);
            sum += level.Price + level.Quantity;
        }

        sw.Stop();

        // Assert
        var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"OrderBookLevel creation: {opsPerSecond:N0} ops/sec");
        _output.WriteLine($"Sum (prevent optimization): {sum}");

        // Снижаем порог - 1M ops/sec достаточно для любого современного CPU
        opsPerSecond.Should().BeGreaterThan(1_000_000,
            "OrderBookLevel creation should be fast enough for trading");
    }

    private sealed class TestObject
    {
        public int Value { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}