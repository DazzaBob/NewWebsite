using System.Text.Json;
using System.Text.Json.Serialization;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal class Feature
        {
            [JsonPropertyName("id")]
            internal string Id { get; set; } = "";

            [JsonPropertyName("type")]
            internal string Type { get; set; } = "";

            [JsonPropertyName("place_type")]
            internal List<string> PlaceType { get; set; } = [];

            [JsonPropertyName("relevance")]
            internal float Relevance { get; set; }

            [JsonPropertyName("properties")]
            internal Dictionary<string, JsonElement> Properties { get; set; } = [];

            // This matches your JSON sample:
            [JsonPropertyName("text_en")]
            internal string Text { get; set; } = "";

            [JsonPropertyName("place_name")]
            internal string PlaceName { get; set; } = "";

            [JsonPropertyName("center")]
            internal List<double>? Center { get; set; }

            [JsonPropertyName("geometry")]
            internal Geometry Geometry { get; set; } = default!;

            [JsonPropertyName("bbox")]
            internal List<double>? BBox { get; set; }

            [JsonPropertyName("context")]
            internal List<ContextItem>? Context { get; set; }
        }
    }
}