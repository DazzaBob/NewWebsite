using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal static class LegacyFeatureParser
    {
        internal class ExtractedAddress
        {
            internal ExtractedAddress() { }
            internal string Locality { get; set; } = string.Empty;
            internal string Place { get; set; } = string.Empty;
            internal string Region { get; set; } = string.Empty;
            internal string Country { get; set; } = string.Empty;
            internal string Postcode { get; set; } = string.Empty;
            internal string PlaceName { get; set; } = string.Empty;
            internal string AddressNumber { get; set; } = string.Empty;
            internal string AddressStreet { get; set; } = string.Empty;
        }
        internal static ExtractedAddress ExtractFullAddress(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return new();

            ExtractedAddress NEA = new();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(rawJson);
                JsonElement feature = doc.RootElement;

                // Primary path: use place_name if present (fast path)
                if (feature.TryGetProperty("place_name", out var placeName) &&
                    placeName.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(placeName.GetString()))
                {
                    NEA.PlaceName = placeName.GetString()!;
                }

                NEA.AddressNumber = feature.TryGetProperty("address", out var addr) ? addr.GetString() ?? "" : "";
                NEA.AddressStreet = feature.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";

                if (feature.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ctx in context.EnumerateArray())
                    {
                        if (ctx.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                        {
                            string id = idProp.GetString() ?? "";

                            if (id.StartsWith("locality.") && ctx.TryGetProperty("text", out var locText))
                                NEA.Locality = locText.GetString() ?? "";
                            else if (id.StartsWith("place.") && ctx.TryGetProperty("text", out var plcText))
                                NEA.Place = plcText.GetString() ?? "";
                            else if (id.StartsWith("region.") && ctx.TryGetProperty("text", out var regText))
                                NEA.Region = regText.GetString() ?? "";
                            else if (id.StartsWith("country.") && ctx.TryGetProperty("text", out var cText))
                                NEA.Country = cText.GetString() ?? "";
                            else if (id.StartsWith("postcode.") && ctx.TryGetProperty("text", out var pcText))
                                NEA.Postcode = pcText.GetString() ?? "";
                        }
                    }
                }
                return NEA;
            }
            catch
            {
                return new();
            }
        }
    }
}