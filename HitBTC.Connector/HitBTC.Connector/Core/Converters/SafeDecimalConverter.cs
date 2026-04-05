// Core/Converters/SafeDecimalConverter.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitBTC.Connector.Core.Converters;

/// <summary>
/// Безопасный конвертер для decimal, который обрабатывает:
/// - null → 0
/// - пустые строки "" → 0
/// - числа как строки "123.45" → 123.45
/// - числа как числа 123.45 → 123.45
/// - некорректные значения → 0
/// </summary>
public class SafeDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadDecimal(ref reader) ?? 0m;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // Записываем как строку для сохранения точности
        writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Читает decimal из JSON. Возвращает null если значение невалидно.
    /// Используется обоими конвертерами.
    /// </summary>
    public static decimal? ReadDecimal(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (reader.TryGetDecimal(out var numberValue))
                    return numberValue;

                // Если не удалось прочитать как decimal (слишком большое/маленькое)
                return null;

            case JsonTokenType.String:
                var stringValue = reader.GetString();

                // Пустая или whitespace строка
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                // Пробуем парсить как decimal
                if (decimal.TryParse(
                    stringValue,
                    System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
                {
                    return parsed;
                }

                // Не удалось парсить (например "Infinity", "NaN" и т.д.)
                return null;

            default:
                // Неожиданный тип токена
                return null;
        }
    }
}

/// <summary>
/// Конвертер для nullable decimal с безопасным парсингом.
/// - null → null
/// - пустые строки "" → null
/// - числа как строки "123.45" → 123.45
/// - некорректные значения → null
/// </summary>
public class SafeNullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return SafeDecimalConverter.ReadDecimal(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}