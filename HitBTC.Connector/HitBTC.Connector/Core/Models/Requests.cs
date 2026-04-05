using System.Globalization;

namespace HitBTC.Connector.Core.Models;

// ----- Spot -----

public sealed class CreateSpotOrderRequest
{
    public string? ClientOrderId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public OrderType Type { get; init; } = OrderType.Limit;
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GTC;
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopPrice { get; init; }
    public DateTime? ExpireTime { get; init; }
    public bool StrictValidate { get; init; }
    public bool PostOnly { get; init; }
    public decimal? DisplayQuantity { get; init; }
    public decimal? TakeRate { get; init; }
    public decimal? MakeRate { get; init; }
}

public sealed class ReplaceSpotOrderRequest
{
    public string? NewClientOrderId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopPrice { get; init; }
    public bool StrictValidate { get; init; }
}

// ----- Futures -----

public sealed class CreateFuturesOrderRequest
{
    public string? ClientOrderId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public OrderType Type { get; init; } = OrderType.Limit;
    public TimeInForce TimeInForce { get; init; } = TimeInForce.GTC;
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopPrice { get; init; }
    public DateTime? ExpireTime { get; init; }
    public bool StrictValidate { get; init; }
    public bool PostOnly { get; init; }
    public bool ReduceOnly { get; init; }
    public bool ClosePosition { get; init; }
    public decimal? DisplayQuantity { get; init; }
    public MarginMode MarginMode { get; init; } = MarginMode.Isolated;
}

public sealed class ReplaceFuturesOrderRequest
{
    public string? NewClientOrderId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopPrice { get; init; }
    public bool StrictValidate { get; init; }
}

public sealed class UpdateMarginAccountRequest
{
    public required decimal MarginBalance { get; init; }
    public decimal? Leverage { get; init; }
    public bool StrictValidate { get; init; }
}

public sealed class ClosePositionRequest
{
    public decimal? Price { get; init; }
    public bool StrictValidate { get; init; }
}

public sealed class HitBtcError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class HitBtcErrorResponse
{
    public HitBtcError? Error { get; set; }
}

// ----- Helper для форматирования decimal -----

public static class DecimalExtensions
{
    /// <summary>
    /// Форматирует decimal без trailing zeros для API запросов.
    /// </summary>
    public static string ToApiString(this decimal value)
    {
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }
}