using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Models;

public sealed class Candle
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("open")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Open { get; set; }

    [JsonPropertyName("close")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Close { get; set; }

    [JsonPropertyName("min")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Low { get; set; }

    [JsonPropertyName("max")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal High { get; set; }

    [JsonPropertyName("volume")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Volume { get; set; }

    [JsonPropertyName("volume_quote")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal VolumeQuote { get; set; }
}