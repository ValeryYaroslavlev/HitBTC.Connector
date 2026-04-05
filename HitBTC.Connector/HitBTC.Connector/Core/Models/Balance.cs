using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Models;

public sealed class FuturesBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Available { get; set; }

    [JsonPropertyName("reserved")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Reserved { get; set; }

    [JsonPropertyName("reserved_margin")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal ReservedMargin { get; set; }

    [JsonPropertyName("cross_margin_reserved")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal CrossMarginReserved { get; set; }
}

public sealed class WalletBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Available { get; set; }

    [JsonPropertyName("reserved")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Reserved { get; set; }
}