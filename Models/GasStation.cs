using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TankenSonstNix.Models;

public class GasStation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;

    // Die Tankerkönig-API liefert die Postleitzahl als Zahl, nicht als String.
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string PostCode { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Dist { get; set; }
    public bool IsOpen { get; set; }

    // Die Tankerkönig-API liefert bei fehlendem Preis den Wert "false" statt einer Zahl.
    // Deshalb ein eigener Converter, der das abfängt statt eine Exception zu werfen.
    [JsonConverter(typeof(NullableDoubleOrFalseConverter))]
    public double? Diesel { get; set; }

    [JsonConverter(typeof(NullableDoubleOrFalseConverter))]
    public double? E5 { get; set; }

    [JsonConverter(typeof(NullableDoubleOrFalseConverter))]
    public double? E10 { get; set; }

    public string Address => $"{Street} {HouseNumber}, {PostCode} {Place}";
    public string DistanceText => $"{Dist.ToString("F1", CultureInfo.InvariantCulture)} km";
    public string DieselText => Diesel.HasValue ? $"{Diesel.Value.ToString("F3", CultureInfo.InvariantCulture)} €" : "—";
    public string E5Text => E5.HasValue ? $"{E5.Value.ToString("F3", CultureInfo.InvariantCulture)} €" : "—";
    public string E10Text => E10.HasValue ? $"{E10.Value.ToString("F3", CultureInfo.InvariantCulture)} €" : "—";
}

public class NullableDoubleOrFalseConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.False or JsonTokenType.True or JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetDouble(),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

public class StringOrNumberConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.String => reader.GetString(),
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
