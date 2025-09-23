using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal static class LegacyFeatureParser
    {
        internal static string ExtractFullAddress(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return string.Empty;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(rawJson);
                JsonElement feature = doc.RootElement;

                // Primary path: full address
                if (feature.TryGetProperty("place_name", out var placeName) &&
                    placeName.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(placeName.GetString()))
                {
                    return placeName.GetString()!;
                }

                string address = feature.TryGetProperty("address", out var addr) ? addr.GetString() ?? "" : "";
                string street = feature.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";

                string locality = "";
                string place = "";
                string region = "";
                string country = "";

                if (feature.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ctx in context.EnumerateArray())
                    {
                        if (ctx.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                        {
                            string id = idProp.GetString() ?? "";

                            if (id.StartsWith("locality.") && ctx.TryGetProperty("text", out var locText))
                                locality = locText.GetString() ?? "";
                            else if (id.StartsWith("place.") && ctx.TryGetProperty("text", out var plcText))
                                place = plcText.GetString() ?? "";
                            else if (id.StartsWith("region.") && ctx.TryGetProperty("text", out var regText))
                                region = regText.GetString() ?? "";
                            else if (id.StartsWith("country.") && ctx.TryGetProperty("text", out var cText))
                                country = cText.GetString() ?? "";
                        }
                    }
                }

                string line1 = string.IsNullOrWhiteSpace(address) ? street : $"{address} {street}";
                string line2 = string.Join(", ", new[] { locality, place, region, country }.Where(s => !string.IsNullOrWhiteSpace(s)));

                return string.IsNullOrWhiteSpace(line2) ? line1 : $"{line1}, {line2}";
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
