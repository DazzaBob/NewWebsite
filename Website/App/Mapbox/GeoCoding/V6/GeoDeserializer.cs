using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V6
{
    internal class GeoDeserializer
    {
        private readonly HttpClient _http;
        private readonly string NamespaceClass = "App.Mapbox.GeoCoding.V6.GeoDeserializer.";
        internal GeoDeserializer()
        {
            _http = new HttpClient();
        }
        internal ResolvedAddressFeature? Search(string address, double? longitude = null, double? latitude = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                if (!longitude.HasValue || !latitude.HasValue) return new ResolvedAddressFeature();
            }

            string url;
            if (!string.IsNullOrWhiteSpace(address))
            {
                url = $"https://api.mapbox.com/search/geocode/v6/forward?q={Uri.EscapeDataString(address)}&limit=1&access_token={Settings.MapboxToken}";
            }
            else
            {
                url = $"https://api.mapbox.com/search/geocode/v6/reverse?longitude={longitude}&latitude={latitude}&limit=1&access_token={Settings.MapboxToken}";
            }
            try
            {
                using HttpResponseMessage response = _http.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                using Stream stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                using JsonDocument doc = JsonDocument.ParseAsync(stream).GetAwaiter().GetResult();
                return ParseFeatureFromCollection(doc);
            }
            catch (Exception ex)
            {
                Website.App.Bootstrap.Logger?.Add(NamespaceClass + $"SearchAsync: {ex.Message}", Helper.Logger.LogLevel.Error);
                return new ResolvedAddressFeature();
            }
        }
        private static ResolvedAddressFeature? ParseFeatureFromCollection(JsonDocument doc)
        {
            if (doc.RootElement.TryGetProperty("features", out var features) &&
                features.ValueKind == JsonValueKind.Array &&
                features.GetArrayLength() > 0)
            {
                JsonElement feature = features.EnumerateArray().First();
                return ResolvedAddressFeature.Parse(feature);
            }
            return null;
        }
    }
}
