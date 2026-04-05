// Core/Models/Balance.cs
using System.Text.Json.Serialization;

using HitBTC.Connector.Core.Converters;

namespace HitBTC.Connector.Core.Models;

public sealed class FuturesBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Available { get; set; }

    [JsonPropertyName("reserved")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Reserved { get; set; }

    [JsonPropertyName("reserved_margin")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal ReservedMargin { get; set; }

    [JsonPropertyName("cross_margin_reserved")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal CrossMarginReserved { get; set; }
}

public sealed class WalletBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("available")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Available { get; set; }

    [JsonPropertyName("reserved")]
    [JsonConverter(typeof(SafeDecimalConverter))]
    public decimal Reserved { get; set; }
}