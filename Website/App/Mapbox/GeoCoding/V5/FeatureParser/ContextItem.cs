using System.Text.Json.Serialization;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal enum FeatureContextType
        {
            Postcode,
            Locality,
            Place,
            Region,
            Country
        }
        internal class ContextItem
        {
            [JsonPropertyName("id")]
            internal string Id { get; set; } = "";

            [JsonPropertyName("mapbox_id")]
            internal string MapboxId { get; set; } = "";

            [JsonPropertyName("text_en")]
            internal string Text { get; set; } = "";

            [JsonPropertyName("wikidata")]
            internal string? Wikidata { get; set; }

            [JsonPropertyName("short_code")]
            internal string? ShortCode { get; set; }
        }
    }
}