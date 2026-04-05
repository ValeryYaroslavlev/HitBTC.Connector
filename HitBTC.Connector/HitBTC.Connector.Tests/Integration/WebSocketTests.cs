// Integration/WebSocketTests.cs
using FluentAssertions;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Tests.Fixtures;
using HitBTC.Connector.WebSocket;

using Xunit;

namespace HitBTC.Connector.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public class WebSocketTests : IAsyncLifetime
{
    private HitBtcWebSocketClient _client = null!;

    public Task InitializeAsync()
    {
        _client = new HitBtcWebSocketClient(WebSocketEndpoint.Public);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task Connect_Public_ShouldSucceed()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Act
        await _client.ConnectAsync();

        // Assert
        _client.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleSubscriptions_ShouldWork()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Arrange
        await _client.ConnectAsync();
        var symbols = new[] { "BTCUSDT", "ETHBTC", "ETHUSDT" };
        var received = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

        _client.OrderBookSnapshotReceived += (symbol, _) =>
        {
            received.TryAdd(symbol, true);
        };

        // Act
        foreach (var symbol in symbols)
        {
            await _client.SubscribeOrderBookAsync(symbol);
        }

        await Task.Delay(5000); // Ждём данные

        // Assert
        received.Keys.Should().Contain(symbols);
    }

    [Fact]
    public async Task Disconnect_ShouldTriggerEvent()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Arrange
        await _client.ConnectAsync();
        var disconnected = new TaskCompletionSource<string>();

        _client.Disconnected += reason =>
        {
            disconnected.TrySetResult(reason);
        };

        // Act
        await _client.DisconnectAsync();

        // Assert
        var reason = await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        reason.Should().NotBeNullOrEmpty();
        _client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Unsubscribe_ShouldStopReceivingData()
    {
        Skip.IfNot(TestConfiguration.RunIntegrationTests, "Integration tests disabled");

        // Arrange
        await _client.ConnectAsync();
        var snapshotCount = 0;

        _client.OrderBookSnapshotReceived += (_, _) =>
        {
            Interlocked.Increment(ref snapshotCount);
        };

        await _client.SubscribeOrderBookAsync(TestConfiguration.TestSymbol);
        await Task.Delay(2000); // Получаем несколько обновлений

        var countBeforeUnsubscribe = snapshotCount;

        // Act
        await _client.UnsubscribeOrderBookAsync(TestConfiguration.TestSymbol);
        await Task.Delay(2000);

        // Assert - после отписки не должно быть новых сообщений
        // (или их должно быть значительно меньше)
        (snapshotCount - countBeforeUnsubscribe).Should().BeLessThan(3);
    }
}