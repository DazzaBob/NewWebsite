using System.Text.Json;

namespace Website.App.Validation
{
    public static partial class AddressValidation
    {
        internal class AddressRecord
        {
            internal string? StreetNumber { get; set; }
            internal string? StreetName { get; set; }
            internal string? Postcode { get; set; }
            internal string? Locality { get; set; }
            internal string? Place { get; set; }
            internal string? Region { get; set; }
            internal string? Country { get; set; }
            internal double Longitude { get; set; }
            internal double Latitude { get; set; }
            internal double RoutableLongitude { get; set; }
            internal double RoutableLatitude { get; set; }
        }
        internal static class RawJsonParser
        {
            internal async static Task<AddressRecord> ParseAddress(string rawJson)
            {
                using JsonDocument doc = JsonDocument.Parse(rawJson);
                JsonElement root = await Task.Run(() => doc.RootElement);

                JsonElement props = await Task.Run(() => root.GetProperty("properties"));
                AddressRecord record = new()
                {
                    StreetNumber = props.GetProperty("address_number").GetString() ?? "",
                    StreetName = props.GetProperty("street").GetString() ?? "",
                    Postcode = props.GetProperty("postcode").GetString() ?? ""
                };

                // Context extraction helper
                string GetContext(string prefix)
                {
                    if (root.TryGetProperty("context", out var ctxArray))
                    {
                        foreach (var item in ctxArray.EnumerateArray())
                        {
                            if (item.GetProperty("id").GetString()?.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase) == true)
                                return item.GetProperty("text_en").GetString() ?? "";
                        }
                    }
                    return "";
                }

                record.Locality = GetContext("locality");
                record.Place = GetContext("place");
                record.Region = GetContext("region");
                record.Country = GetContext("country");

                // Geometry coordinates
                var coords = root
                    .GetProperty("geometry")
                    .GetProperty("coordinates")
                    .EnumerateArray()
                    .Select(e => e.GetDouble())
                    .ToArray();

                if (coords.Length >= 2)
                {
                    record.Longitude = coords[0];
                    record.Latitude = coords[1];
                    record.RoutableLongitude = coords[0];
                    record.RoutableLatitude = coords[1];
                }

                return record;
            }
        }
    }
}
