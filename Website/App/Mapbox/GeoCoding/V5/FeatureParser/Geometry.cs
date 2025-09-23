using System.Text.Json.Serialization;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal class Geometry
        {
            [JsonPropertyName("type")]
            internal string? Type { get; set; }

            [JsonPropertyName("coordinates")]
            internal List<double>? Coordinates { get; set; }
        }
    }
}
