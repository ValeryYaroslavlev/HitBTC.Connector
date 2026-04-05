// Rest/IHitBtcRestClient.cs
using HitBTC.Connector.Core.Models;

namespace HitBTC.Connector.Rest;

/// <summary>
/// Интерфейс REST клиента. Разделён по принципу ISP (Interface Segregation).
/// </summary>
public interface IHitBtcPublicRestClient
{
    Task<Dictionary<string, SymbolInfo>> GetSymbolsAsync(
        string[]? symbols = null, CancellationToken ct = default);

    Task<SymbolInfo?> GetSymbolAsync(
        string symbol, CancellationToken ct = default);

    Task<OrderBook> GetOrderBookAsync(
        string symbol, int depth = 100, CancellationToken ct = default);

    Task<Candle[]> GetCandlesAsync(
        string symbol,
        CandlePeriod period = CandlePeriod.M30,
        SortDirection sort = SortDirection.Desc,
        DateTime? from = null,
        DateTime? till = null,
        int limit = 100,
        int offset = 0,
        CancellationToken ct = default);
}

public interface IHitBtcSpotRestClient
{
    Task<SpotOrder[]> GetActiveSpotOrdersAsync(
        string? symbol = null, CancellationToken ct = default);

    Task<SpotOrder> GetSpotOrderAsync(
        string clientOrderId, CancellationToken ct = default);

    Task<SpotOrder> CreateSpotOrderAsync(
        CreateSpotOrderRequest request, CancellationToken ct = default);

    Task<SpotOrder> ReplaceSpotOrderAsync(
        string clientOrderId, ReplaceSpotOrderRequest request, CancellationToken ct = default);

    Task<SpotOrder> CancelSpotOrderAsync(
        string clientOrderId, CancellationToken ct = default);

    Task<SpotOrder[]> CancelAllSpotOrdersAsync(
        string? symbol = null, CancellationToken ct = default);
}

public interface IHitBtcFuturesRestClient
{
    Task<FuturesBalance[]> GetFuturesBalanceAsync(CancellationToken ct = default);

    Task<FuturesBalance?> GetFuturesBalanceAsync(
        string currency, CancellationToken ct = default);

    Task<FuturesMarginAccount[]> GetFuturesAccountsAsync(CancellationToken ct = default);

    Task<FuturesMarginAccount> GetFuturesAccountAsync(
        string symbol, CancellationToken ct = default);

    // ===== Margin Account Management =====

    /// <summary>
    /// Creates or updates a futures margin account.
    /// </summary>
    Task<FuturesMarginAccount> CreateOrUpdateMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        decimal marginBalance,
        decimal? leverage = null,
        bool strictValidate = false,
        CancellationToken ct = default);

    /// <summary>
    /// Closes margin account and returns all funds to trading account.
    /// </summary>
    Task<FuturesMarginAccount> CloseMarginAccountAsync(
        MarginMode marginMode,
        string symbol,
        CancellationToken ct = default);

    /// <summary>
    /// Updates leverage for isolated margin account.
    /// </summary>
    Task<FuturesMarginAccount> UpdateLeverageAsync(
        string symbol,
        decimal leverage,
        CancellationToken ct = default);

    /// <summary>
    /// Adds funds to margin account.
    /// </summary>
    Task<FuturesMarginAccount> AddMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default);

    /// <summary>
    /// Removes funds from margin account.
    /// </summary>
    Task<FuturesMarginAccount> RemoveMarginAsync(
        MarginMode marginMode,
        string symbol,
        decimal amount,
        CancellationToken ct = default);

    /// <summary>
    /// Legacy method - use CreateOrUpdateMarginAccountAsync instead.
    /// </summary>
    Task<FuturesMarginAccount> UpdateFuturesAccountAsync(
        MarginMode marginMode, string symbol,
        UpdateMarginAccountRequest request, CancellationToken ct = default);

    // ===== Positions =====

    Task<MarginPosition[]> CloseAllFuturesPositionsAsync(CancellationToken ct = default);

    Task<MarginPosition[]> CloseFuturesPositionAsync(
        MarginMode marginMode, string symbol,
        ClosePositionRequest? request = null, CancellationToken ct = default);

    // ===== Orders =====

    Task<FuturesOrder[]> GetActiveFuturesOrdersAsync(
        string? symbol = null, CancellationToken ct = default);

    Task<FuturesOrder> GetFuturesOrderAsync(
        string clientOrderId, CancellationToken ct = default);

    Task<FuturesOrder> CreateFuturesOrderAsync(
        CreateFuturesOrderRequest request, CancellationToken ct = default);

    Task<FuturesOrder> ReplaceFuturesOrderAsync(
        string clientOrderId, ReplaceFuturesOrderRequest request, CancellationToken ct = default);

    Task<FuturesOrder> CancelFuturesOrderAsync(
        string clientOrderId, CancellationToken ct = default);

    Task<FuturesOrder[]> CancelAllFuturesOrdersAsync(
        string? symbol = null, CancellationToken ct = default);
}

public interface IHitBtcRestClient :
    IHitBtcPublicRestClient,
    IHitBtcSpotRestClient,
    IHitBtcFuturesRestClient,
    IAsyncDisposable
{
}