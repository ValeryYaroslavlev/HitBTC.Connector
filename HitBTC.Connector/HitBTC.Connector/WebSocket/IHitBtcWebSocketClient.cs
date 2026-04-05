// WebSocket/IHitBtcWebSocketClient.cs
using HitBTC.Connector.Core.Models;

namespace HitBTC.Connector.WebSocket;

public interface IHitBtcWebSocketClient : IAsyncDisposable
{
    event Action<string, OrderBookSnapshot>? OrderBookSnapshotReceived;
    event Action<string, OrderBookSnapshot>? OrderBookUpdateReceived;
    event Action<string, PublicTrade[]>? TradesReceived;
    event Action<SpotOrder>? SpotOrderUpdated;
    event Action<FuturesOrder>? FuturesOrderUpdated;
    event Action<string>? RawMessageReceived;
    event Action<Exception>? ErrorOccurred;
    event Action? Connected;
    event Action<string>? Disconnected;

    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    Task SubscribeOrderBookAsync(string symbol, CancellationToken ct = default);
    Task SubscribeTradesAsync(string symbol, CancellationToken ct = default);
    Task SubscribeCandlesAsync(string symbol, CandlePeriod period, CancellationToken ct = default);

    Task UnsubscribeOrderBookAsync(string symbol, CancellationToken ct = default);
    Task UnsubscribeTradesAsync(string symbol, CancellationToken ct = default);

    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);
}