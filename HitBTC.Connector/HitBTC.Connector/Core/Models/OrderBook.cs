// Core/Models/OrderBook.cs
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Models;

[StructLayout(LayoutKind.Auto)]
public readonly record struct OrderBookLevel(decimal Price, decimal Quantity);

public sealed class OrderBookSnapshot
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("ask")]
    public string[][] Ask { get; set; } = Array.Empty<string[]>();

    [JsonPropertyName("bid")]
    public string[][] Bid { get; set; } = Array.Empty<string[]>();
}

/// <summary>
/// OrderBook с оптимизированным хранением через ArrayPool.
/// </summary>
public sealed class OrderBook : IDisposable
{
    private OrderBookLevel[] _asks;
    private OrderBookLevel[] _bids;
    private int _askCount;
    private int _bidCount;
    private bool _disposed;

    public DateTime Timestamp { get; set; }
    public int AskCount => _askCount;
    public int BidCount => _bidCount;

    public ReadOnlySpan<OrderBookLevel> Asks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _asks.AsSpan(0, _askCount);
    }

    public ReadOnlySpan<OrderBookLevel> Bids
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bids.AsSpan(0, _bidCount);
    }

    public OrderBook(int initialCapacity = 128)
    {
        _asks = ArrayPool<OrderBookLevel>.Shared.Rent(initialCapacity);
        _bids = ArrayPool<OrderBookLevel>.Shared.Rent(initialCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAsks(ReadOnlySpan<OrderBookLevel> asks)
    {
        EnsureCapacity(ref _asks, asks.Length);
        asks.CopyTo(_asks);
        _askCount = asks.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBids(ReadOnlySpan<OrderBookLevel> bids)
    {
        EnsureCapacity(ref _bids, bids.Length);
        bids.CopyTo(_bids);
        _bidCount = bids.Length;
    }

    public static OrderBook FromSnapshot(OrderBookSnapshot snapshot)
    {
        var ob = new OrderBook(Math.Max(snapshot.Ask.Length, snapshot.Bid.Length));

        // Парсим как UTC
        if (DateTimeOffset.TryParse(
            snapshot.Timestamp,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var ts))
        {
            ob.Timestamp = ts.UtcDateTime;
        }

        // ... остальной код без изменений
        Span<OrderBookLevel> asks = stackalloc OrderBookLevel[snapshot.Ask.Length];
        for (int i = 0; i < snapshot.Ask.Length; i++)
        {
            if (snapshot.Ask[i].Length >= 2 &&
                decimal.TryParse(snapshot.Ask[i][0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var askPrice) &&
                decimal.TryParse(snapshot.Ask[i][1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var askQty))
            {
                asks[i] = new OrderBookLevel(askPrice, askQty);
            }
        }
        ob.SetAsks(asks);

        Span<OrderBookLevel> bids = stackalloc OrderBookLevel[snapshot.Bid.Length];
        for (int i = 0; i < snapshot.Bid.Length; i++)
        {
            if (snapshot.Bid[i].Length >= 2 &&
                decimal.TryParse(snapshot.Bid[i][0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var bidPrice) &&
                decimal.TryParse(snapshot.Bid[i][1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var bidQty))
            {
                bids[i] = new OrderBookLevel(bidPrice, bidQty);
            }
        }
        ob.SetBids(bids);

        return ob;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCapacity(ref OrderBookLevel[] array, int required)
    {
        if (array.Length >= required) return;

        var old = array;
        array = ArrayPool<OrderBookLevel>.Shared.Rent(required * 2);
        ArrayPool<OrderBookLevel>.Shared.Return(old);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ArrayPool<OrderBookLevel>.Shared.Return(_asks);
        ArrayPool<OrderBookLevel>.Shared.Return(_bids);
    }
}