using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TankenSonstNix.Models;

namespace TankenSonstNix.Services;

public class TankerkoenigService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://creativecommons.tankerkoenig.de/json/list.php";

    public TankerkoenigService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Liefert die bis zu 10 nächstgelegenen Tankstellen im angegebenen Radius, sortiert nach Entfernung.
    /// </summary>
    public async Task<List<GasStation>> GetNearbyStationsAsync(double lat, double lng, string apiKey, double radiusKm = 25)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API-Key darf nicht leer sein.", nameof(apiKey));

        var url = $"{BaseUrl}?lat={lat.ToString(CultureInfo.InvariantCulture)}" +
                   $"&lng={lng.ToString(CultureInfo.InvariantCulture)}" +
                   $"&rad={radiusKm.ToString(CultureInfo.InvariantCulture)}" +
                   $"&sort=dist&type=all&apikey={apiKey}";

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TankerkoenigResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null || !result.Ok)
        {
            var message = result?.Message ?? "Unbekannte Antwort";
            if (message.Contains("apikey", StringComparison.OrdinalIgnoreCase))
                throw new TankerkoenigApiKeyException(message);

            throw new InvalidOperationException($"Tankerkönig-API Fehler: {message}");
        }

        var stations = result.Stations ?? new List<GasStation>();

        return stations
            .OrderBy(s => s.Dist)
            .Take(10)
            .ToList();
    }

    private class TankerkoenigResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("stations")]
        public List<GasStation>? Stations { get; set; }
    }
}

public class TankerkoenigApiKeyException : Exception
{
    public TankerkoenigApiKeyException(string message) : base(message)
    {
    }
}
