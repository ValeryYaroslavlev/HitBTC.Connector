// Trading/MarketDataManager.cs
using System.Collections.Concurrent;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.WebSocket;

namespace HitBTC.Connector.Trading;

/// <summary>
/// Менеджер рыночных данных с lock-free order book management.
/// Сочетает REST snapshots + WebSocket incremental updates.
/// </summary>
public sealed class MarketDataManager : IAsyncDisposable
{
    private readonly HitBtcRestClient _rest;
    private readonly HitBtcWebSocketClient _ws;
    private readonly ConcurrentDictionary<string, OrderBook> _orderBooks;
    private readonly ConcurrentDictionary<string, SymbolInfo> _symbols;

    public event Action<string, OrderBook>? OrderBookChanged;
    public event Action<string, PublicTrade[]>? TradesReceived;
    public event Action<Exception>? Error;

    public MarketDataManager(string? apiKey = null, string? secretKey = null)
    {
        _rest = new HitBtcRestClient(apiKey ?? string.Empty, secretKey ?? string.Empty);
        _ws = new HitBtcWebSocketClient(WebSocketEndpoint.Public);
        _orderBooks = new ConcurrentDictionary<string, OrderBook>();
        _symbols = new ConcurrentDictionary<string, SymbolInfo>();

        _ws.OrderBookSnapshotReceived += OnOrderBookSnapshot;
        _ws.OrderBookUpdateReceived += OnOrderBookUpdate;
        _ws.TradesReceived += OnTradesReceived;
        _ws.ErrorOccurred += ex => Error?.Invoke(ex);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _ws.ConnectAsync(ct);

        // Загружаем список символов
        var symbols = await _rest.GetSymbolsAsync(ct: ct);
        foreach (var (code, info) in symbols)
        {
            _symbols.TryAdd(code, info);
        }
    }

    public IReadOnlyDictionary<string, SymbolInfo> Symbols => _symbols;

    /// <summary>
    /// Подписка на OrderBook. Сначала загружает snapshot через REST, потом WebSocket updates.
    /// </summary>
    public async Task SubscribeOrderBookAsync(
        string symbol, int depth = 100, CancellationToken ct = default)
    {
        // REST snapshot
        var ob = await _rest.GetOrderBookAsync(symbol, depth, ct);
        _orderBooks.AddOrUpdate(symbol, ob, (_, old) =>
        {
            old.Dispose();
            return ob;
        });

        // WebSocket subscription
        await _ws.SubscribeOrderBookAsync(symbol, ct);
    }

    public async Task SubscribeTradesAsync(
        string symbol, CancellationToken ct = default)
    {
        await _ws.SubscribeTradesAsync(symbol, ct);
    }

    /// <summary>
    /// Получение текущего OrderBook. Zero-copy — возвращает ссылку.
    /// </summary>
    public OrderBook? GetOrderBook(string symbol)
    {
        _orderBooks.TryGetValue(symbol, out var ob);
        return ob;
    }

    public SymbolInfo? GetSymbol(string symbol)
    {
        _symbols.TryGetValue(symbol, out var info);
        return info;
    }

    public async Task<Candle[]> GetCandlesAsync(
        string symbol,
        CandlePeriod period = CandlePeriod.M30,
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _rest.GetCandlesAsync(symbol, period, limit: limit, ct: ct);
    }

    // =================== WS HANDLERS ===================

    private void OnOrderBookSnapshot(string symbol, OrderBookSnapshot snapshot)
    {
        var ob = OrderBook.FromSnapshot(snapshot);
        _orderBooks.AddOrUpdate(symbol, ob, (_, old) =>
        {
            old.Dispose();
            return ob;
        });
        OrderBookChanged?.Invoke(symbol, ob);
    }

    private void OnOrderBookUpdate(string symbol, OrderBookSnapshot update)
    {
        // Для инкрементальных обновлений — применяем delta
        if (!_orderBooks.TryGetValue(symbol, out var existing))
        {
            // Нет snapshot — создаём из update
            var ob = OrderBook.FromSnapshot(update);
            _orderBooks.TryAdd(symbol, ob);
            OrderBookChanged?.Invoke(symbol, ob);
            return;
        }

        // Применяем обновление
        ApplyOrderBookUpdate(existing, update);
        OrderBookChanged?.Invoke(symbol, existing);
    }

    private static void ApplyOrderBookUpdate(OrderBook ob, OrderBookSnapshot update)
    {
        if (DateTime.TryParse(update.Timestamp, out var ts))
            ob.Timestamp = ts;

        // Для полного OrderBook channel — полная замена уровней
        if (update.Ask.Length > 0)
        {
            Span<OrderBookLevel> asks = new OrderBookLevel[update.Ask.Length];
            for (int i = 0; i < update.Ask.Length; i++)
            {
                if (update.Ask[i].Length >= 2 &&
                    decimal.TryParse(update.Ask[i][0],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var p) &&
                    decimal.TryParse(update.Ask[i][1],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var q))
                {
                    asks[i] = new OrderBookLevel(p, q);
                }
            }
            ob.SetAsks(asks);
        }

        if (update.Bid.Length > 0)
        {
            Span<OrderBookLevel> bids = new OrderBookLevel[update.Bid.Length];
            for (int i = 0; i < update.Bid.Length; i++)
            {
                if (update.Bid[i].Length >= 2 &&
                    decimal.TryParse(update.Bid[i][0],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var p) &&
                    decimal.TryParse(update.Bid[i][1],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var q))
                {
                    bids[i] = new OrderBookLevel(p, q);
                }
            }
            ob.SetBids(bids);
        }
    }

    private void OnTradesReceived(string symbol, PublicTrade[] trades)
    {
        TradesReceived?.Invoke(symbol, trades);
    }

    public async ValueTask DisposeAsync()
    {
        _ws.OrderBookSnapshotReceived -= OnOrderBookSnapshot;
        _ws.OrderBookUpdateReceived -= OnOrderBookUpdate;
        _ws.TradesReceived -= OnTradesReceived;

        await _ws.DisposeAsync();
        await _rest.DisposeAsync();

        foreach (var ob in _orderBooks.Values)
        {
            ob.Dispose();
        }
        _orderBooks.Clear();
    }
}