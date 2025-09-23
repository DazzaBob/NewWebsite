namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal class ResolvedAddressFeature
        {
            internal required string FeatureId { get; set; }
            internal required string FeatureType { get; set; }
            internal string SourceType { get; set; } = "";
            internal float Relevance { get; set; }

            internal string FullAddress { get; set; } = "";
            internal string StreetName { get; set; } = "";
            internal string StreetNumber { get; set; } = "";

            internal GeoCoordinates Coords { get; set; } = new();
            internal GeoBoundingBox? BBox { get; set; }

            internal Dictionary<string, string> RawProperties { get; set; } = [];

            internal GeoContext Postcode { get; set; } = new();
            internal GeoContext Locality { get; set; } = new();
            internal GeoContext Place { get; set; } = new();
            internal GeoContext Region { get; set; } = new();
            internal GeoContext Country { get; set; } = new();
        }

        internal class GeoCoordinates
        {
            internal double DisplayLatitude { get; set; }
            internal double DisplayLongitude { get; set; }
            internal double RoutableLatitude { get; set; }
            internal double RoutableLongitude { get; set; }
        }

        internal class GeoBoundingBox
        {
            internal double MinLongitude { get; set; }
            internal double MinLatitude { get; set; }
            internal double MaxLongitude { get; set; }
            internal double MaxLatitude { get; set; }
        }

        internal class GeoContext
        {
            internal string Name { get; set; } = "";
            internal string WikidataId { get; set; } = "";
            internal string ShortCode { get; set; } = "";
        }
    }
}