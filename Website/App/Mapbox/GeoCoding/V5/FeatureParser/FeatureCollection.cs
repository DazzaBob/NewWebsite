using System.Text.Json.Serialization;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal class FeatureCollection
        {
            [JsonPropertyName("type")]
            internal string? Type { get; set; }

            [JsonPropertyName("query")]
            internal List<string>? Query { get; set; }

            [JsonPropertyName("features")]
            internal List<Feature>? Features { get; set; }

            [JsonPropertyName("attribution")]
            internal string? Attribution { get; set; }
        }
    }
}