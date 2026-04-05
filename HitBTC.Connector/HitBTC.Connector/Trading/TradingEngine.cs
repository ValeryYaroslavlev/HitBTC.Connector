// Trading/TradingEngine.cs
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using HitBTC.Connector.Core.Models;
using HitBTC.Connector.Rest;
using HitBTC.Connector.WebSocket;

namespace HitBTC.Connector.Trading;

/// <summary>
/// High-performance Trading Engine.
/// Отслеживает ордера через WebSocket и REST.
/// Lock-free order tracking через ConcurrentDictionary.
/// </summary>
public sealed class TradingEngine : IAsyncDisposable
{
    private readonly IHitBtcRestClient _rest;
    private readonly IHitBtcWebSocketClient _ws;
    private readonly ConcurrentDictionary<string, SpotOrder> _activeSpotOrders;
    private readonly ConcurrentDictionary<string, FuturesOrder> _activeFuturesOrders;
    private readonly CancellationTokenSource _cts;

    // Events
    public event Action<SpotOrder>? SpotOrderPlaced;
    public event Action<SpotOrder>? SpotOrderFilled;
    public event Action<SpotOrder>? SpotOrderCanceled;
    public event Action<SpotOrder>? SpotOrderUpdated;

    public event Action<FuturesOrder>? FuturesOrderPlaced;
    public event Action<FuturesOrder>? FuturesOrderFilled;
    public event Action<FuturesOrder>? FuturesOrderCanceled;
    public event Action<FuturesOrder>? FuturesOrderUpdated;

    public event Action<Exception>? Error;

    public TradingEngine(string apiKey, string secretKey)
    {
        _rest = new HitBtcRestClient(apiKey, secretKey);
        _ws = new HitBtcWebSocketClient(WebSocketEndpoint.Trading, apiKey, secretKey);

        _activeSpotOrders = new ConcurrentDictionary<string, SpotOrder>();
        _activeFuturesOrders = new ConcurrentDictionary<string, FuturesOrder>();
        _cts = new CancellationTokenSource();

        _ws.SpotOrderUpdated += OnSpotOrderUpdated;
        _ws.FuturesOrderUpdated += OnFuturesOrderUpdated;
        _ws.ErrorOccurred += ex => Error?.Invoke(ex);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _ws.ConnectAsync(ct);

        // Загружаем текущие активные ордера
        var spotOrders = await _rest.GetActiveSpotOrdersAsync(ct: ct);
        foreach (var order in spotOrders)
        {
            _activeSpotOrders.TryAdd(order.ClientOrderId, order);
        }

        var futuresOrders = await _rest.GetActiveFuturesOrdersAsync(ct: ct);
        foreach (var order in futuresOrders)
        {
            _activeFuturesOrders.TryAdd(order.ClientOrderId, order);
        }
    }

    // =================== SPOT ===================

    public IReadOnlyDictionary<string, SpotOrder> ActiveSpotOrders => _activeSpotOrders;

    public async Task<SpotOrder> PlaceSpotOrderAsync(
        CreateSpotOrderRequest request, CancellationToken ct = default)
    {
        var order = await _rest.CreateSpotOrderAsync(request, ct);
        _activeSpotOrders.TryAdd(order.ClientOrderId, order);
        SpotOrderPlaced?.Invoke(order);
        return order;
    }

    public async Task<SpotOrder> CancelSpotOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        var order = await _rest.CancelSpotOrderAsync(clientOrderId, ct);
        _activeSpotOrders.TryRemove(clientOrderId, out _);
        SpotOrderCanceled?.Invoke(order);
        return order;
    }

    public async Task<SpotOrder[]> CancelAllSpotOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var orders = await _rest.CancelAllSpotOrdersAsync(symbol, ct);
        foreach (var order in orders)
        {
            _activeSpotOrders.TryRemove(order.ClientOrderId, out _);
            SpotOrderCanceled?.Invoke(order);
        }
        return orders;
    }

    public async Task<SpotOrder> ReplaceSpotOrderAsync(
        string clientOrderId, ReplaceSpotOrderRequest request, CancellationToken ct = default)
    {
        var order = await _rest.ReplaceSpotOrderAsync(clientOrderId, request, ct);

        _activeSpotOrders.TryRemove(clientOrderId, out _);
        _activeSpotOrders.TryAdd(order.ClientOrderId, order);

        SpotOrderUpdated?.Invoke(order);
        return order;
    }

    // =================== FUTURES ===================

    public IReadOnlyDictionary<string, FuturesOrder> ActiveFuturesOrders => _activeFuturesOrders;

    public async Task<FuturesOrder> PlaceFuturesOrderAsync(
        CreateFuturesOrderRequest request, CancellationToken ct = default)
    {
        var order = await _rest.CreateFuturesOrderAsync(request, ct);
        _activeFuturesOrders.TryAdd(order.ClientOrderId, order);
        FuturesOrderPlaced?.Invoke(order);
        return order;
    }

    public async Task<FuturesOrder> CancelFuturesOrderAsync(
        string clientOrderId, CancellationToken ct = default)
    {
        var order = await _rest.CancelFuturesOrderAsync(clientOrderId, ct);
        _activeFuturesOrders.TryRemove(clientOrderId, out _);
        FuturesOrderCanceled?.Invoke(order);
        return order;
    }

    public async Task<FuturesOrder[]> CancelAllFuturesOrdersAsync(
        string? symbol = null, CancellationToken ct = default)
    {
        var orders = await _rest.CancelAllFuturesOrdersAsync(symbol, ct);
        foreach (var order in orders)
        {
            _activeFuturesOrders.TryRemove(order.ClientOrderId, out _);
            FuturesOrderCanceled?.Invoke(order);
        }
        return orders;
    }

    public async Task<FuturesOrder> ReplaceFuturesOrderAsync(
        string clientOrderId, ReplaceFuturesOrderRequest request, CancellationToken ct = default)
    {
        var order = await _rest.ReplaceFuturesOrderAsync(clientOrderId, request, ct);

        _activeFuturesOrders.TryRemove(clientOrderId, out _);
        _activeFuturesOrders.TryAdd(order.ClientOrderId, order);

        FuturesOrderUpdated?.Invoke(order);
        return order;
    }

    // =================== POSITIONS ===================

    public Task<FuturesMarginAccount[]> GetFuturesAccountsAsync(CancellationToken ct = default)
        => _rest.GetFuturesAccountsAsync(ct);

    public Task<FuturesMarginAccount> UpdateFuturesAccountAsync(
        MarginMode mode, string symbol,
        UpdateMarginAccountRequest request, CancellationToken ct = default)
        => _rest.UpdateFuturesAccountAsync(mode, symbol, request, ct);

    public Task<MarginPosition[]> CloseAllPositionsAsync(CancellationToken ct = default)
        => _rest.CloseAllFuturesPositionsAsync(ct);

    public Task<MarginPosition[]> ClosePositionAsync(
        MarginMode mode, string symbol,
        ClosePositionRequest? request = null, CancellationToken ct = default)
        => _rest.CloseFuturesPositionAsync(mode, symbol, request, ct);

    // =================== MARGIN ACCOUNT MANAGEMENT ===================

    /// <summary>
    /// Creates or updates a futures margin account with specified balance and leverage.
    /// </summary>
    public Task<FuturesMarginAccount> CreateOrUpdateMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        decimal marginBalance,
        decimal? leverage = null,
        bool strictValidate = false,
        CancellationToken ct = default)
        => _rest.CreateOrUpdateMarginAccountAsync(
            marginMode, symbol, marginBalance, leverage, strictValidate, ct);

    /// <summary>
    /// Closes margin account and withdraws all funds.
    /// </summary>
    public Task<FuturesMarginAccount> CloseMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        CancellationToken ct = default)
        => _rest.CloseMarginAccountAsync(marginMode, symbol, ct);

    /// <summary>
    /// Updates leverage for isolated margin account.
    /// </summary>
    public Task<FuturesMarginAccount> UpdateLeverageAsync(
        string symbol,
        decimal leverage,
        CancellationToken ct = default)
        => _rest.UpdateLeverageAsync(symbol, leverage, ct);

    /// <summary>
    /// Adds margin to existing account.
    /// </summary>
    public Task<FuturesMarginAccount> AddMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default)
        => _rest.AddMarginAsync(marginMode, symbol, amount, ct);

    /// <summary>
    /// Removes margin from account.
    /// </summary>
    public Task<FuturesMarginAccount> RemoveMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default)
        => _rest.RemoveMarginAsync(marginMode, symbol, amount, ct);

    // =================== WS CALLBACKS ===================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnSpotOrderUpdated(SpotOrder order)
    {
        switch (order.Status)
        {
            case OrderStatus.New:
            case OrderStatus.PartiallyFilled:
            case OrderStatus.Suspended:
                _activeSpotOrders.AddOrUpdate(
                    order.ClientOrderId, order, (_, _) => order);
                SpotOrderUpdated?.Invoke(order);
                break;

            case OrderStatus.Filled:
                _activeSpotOrders.TryRemove(order.ClientOrderId, out _);
                SpotOrderFilled?.Invoke(order);
                break;

            case OrderStatus.Canceled:
            case OrderStatus.Expired:
                _activeSpotOrders.TryRemove(order.ClientOrderId, out _);
                SpotOrderCanceled?.Invoke(order);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnFuturesOrderUpdated(FuturesOrder order)
    {
        switch (order.Status)
        {
            case OrderStatus.New:
            case OrderStatus.PartiallyFilled:
            case OrderStatus.Suspended:
                _activeFuturesOrders.AddOrUpdate(
                    order.ClientOrderId, order, (_, _) => order);
                FuturesOrderUpdated?.Invoke(order);
                break;

            case OrderStatus.Filled:
                _activeFuturesOrders.TryRemove(order.ClientOrderId, out _);
                FuturesOrderFilled?.Invoke(order);
                break;

            case OrderStatus.Canceled:
            case OrderStatus.Expired:
                _activeFuturesOrders.TryRemove(order.ClientOrderId, out _);
                FuturesOrderCanceled?.Invoke(order);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        _ws.SpotOrderUpdated -= OnSpotOrderUpdated;
        _ws.FuturesOrderUpdated -= OnFuturesOrderUpdated;

        await _ws.DisposeAsync();
        await _rest.DisposeAsync();

        _cts.Dispose();
    }
}