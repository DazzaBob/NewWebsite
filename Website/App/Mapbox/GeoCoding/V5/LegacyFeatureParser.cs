using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal static class LegacyFeatureParser
    {
        internal class ExtractedAddress
        {
            internal string Locality { get; set; } = string.Empty;
            internal string Place { get; set; } = string.Empty;
            internal string Region { get; set; } = string.Empty;
            internal string Country { get; set; } = string.Empty;
            internal string Postcode { get; set; } = string.Empty;
            internal string PlaceName { get; set; } = string.Empty;
            internal string AddressNumber { get; set; } = string.Empty;
            internal string AddressStreet { get; set; } = string.Empty;

            /// <summary>
            /// Returns a nicely formatted address string.
            /// Prioritises PlaceName (which is usually complete).
            /// Falls back to manual composition if needed.
            /// </summary>
            internal string Formatted
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(PlaceName))
                        return PlaceName;

                    var parts = new List<string>();

                    var street = $"{AddressNumber} {AddressStreet}".Trim();
                    if (!string.IsNullOrWhiteSpace(street))
                        parts.Add(street);

                    if (!string.IsNullOrWhiteSpace(Locality))
                        parts.Add(Locality);

                    if (!string.IsNullOrWhiteSpace(Place))
                        parts.Add(Place);

                    if (!string.IsNullOrWhiteSpace(Postcode))
                        parts.Add(Postcode);

                    if (!string.IsNullOrWhiteSpace(Country))
                        parts.Add(Country);

                    return string.Join(", ", parts);
                }
            }
        }
        internal static ExtractedAddress ExtractFullAddress(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return new();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(rawJson);
                JsonElement root = doc.RootElement;

                // 1. Handle location modal JSON (wrapped feature)
                if (root.TryGetProperty("feature", out var featureElement) && featureElement.ValueKind == JsonValueKind.Object)
                    return ParseFeature(featureElement);

                // 2. Handle address modal JSON (FeatureCollection)
                if (root.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array && features.GetArrayLength() > 0)
                    return ParseFeature(features[0]);

                // 3. Handle legacy direct feature JSON
                return ParseFeature(root);
            }
            catch
            {
                return new();
            }
        }
        private static ExtractedAddress ParseFeature(JsonElement feature)
        {
            var result = new ExtractedAddress();

            if (feature.TryGetProperty("place_name", out var placeName) && placeName.ValueKind == JsonValueKind.String)
                result.PlaceName = placeName.GetString() ?? "";

            if (feature.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.String)
                result.AddressNumber = addr.GetString() ?? "";

            if (feature.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
                result.AddressStreet = txt.GetString() ?? "";

            if (feature.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.Array)
            {
                foreach (var ctx in context.EnumerateArray())
                {
                    if (!ctx.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                        continue;

                    string id = idProp.GetString() ?? "";
                    if (id.StartsWith("locality.") && ctx.TryGetProperty("text", out var locText))
                        result.Locality = locText.GetString() ?? "";
                    else if (id.StartsWith("place.") && ctx.TryGetProperty("text", out var plcText))
                        result.Place = plcText.GetString() ?? "";
                    else if (id.StartsWith("region.") && ctx.TryGetProperty("text", out var regText))
                        result.Region = regText.GetString() ?? "";
                    else if (id.StartsWith("country.") && ctx.TryGetProperty("text", out var cText))
                        result.Country = cText.GetString() ?? "";
                    else if (id.StartsWith("postcode.") && ctx.TryGetProperty("text", out var pcText))
                        result.Postcode = pcText.GetString() ?? "";
                }
            }

            return result;
        }
    }
}