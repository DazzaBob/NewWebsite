using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal partial class FeatureParser
    {
        internal static ResolvedAddressFeature ParseResolvedFeature(Feature feature)
        {
            var resolved = new ResolvedAddressFeature
            {
                FeatureId = feature.Id,
                FeatureType = feature.Type,
                SourceType = feature.PlaceType?.FirstOrDefault() ?? "",
                Relevance = feature.Relevance,
                FullAddress = feature.PlaceName,
                StreetName = feature.Text,
                // pull street number from properties["address_number"]
                StreetNumber = feature.Properties.TryGetValue("address_number", out var num)
                                   ? num.GetString() ?? ""
                                   : "",
                Coords = new GeoCoordinates()
            };

            // ■ Display coords come from Mapbox "center"
            if (feature.Center != null && feature.Center.Count >= 2)
            {
                resolved.Coords.DisplayLatitude = feature.Center[1];
                resolved.Coords.DisplayLongitude = feature.Center[0];
            }

            // ■ Routable coords come from GeoJSON geometry (lon, lat)
            var raw = feature.Geometry?.Coordinates;
            if (raw != null && raw.Count >= 2)
            {
                resolved.Coords.RoutableLatitude = raw[1];
                resolved.Coords.RoutableLongitude = raw[0];
            }

            // ■ Bounding box
            if (feature.BBox?.Count == 4)
            {
                resolved.BBox = new GeoBoundingBox
                {
                    MinLongitude = feature.BBox[0],
                    MinLatitude = feature.BBox[1],
                    MaxLongitude = feature.BBox[2],
                    MaxLatitude = feature.BBox[3]
                };
            }

            // ■ Dump all other props
            foreach (var kvp in feature.Properties)
            {
                if (kvp.Value.ValueKind != JsonValueKind.Null)
                    resolved.RawProperties[kvp.Key] = kvp.Value.ToString()!;
            }

            // ■ Build each context level
            resolved.Postcode = BuildContext(feature, "postcode");
            resolved.Locality = BuildContext(feature, "locality");
            resolved.Place = BuildContext(feature, "place");
            resolved.Region = BuildContext(feature, "region");
            resolved.Country = BuildContext(feature, "country");

            return resolved;
        }
        private static FeatureParser.GeoContext BuildContext(Feature f, string prefix)
        {
            var match = f.Context?
                .FirstOrDefault(c => c.Id.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));

            return new GeoContext
            {
                Name = match?.Text ?? "",
                ShortCode = match?.ShortCode ?? "",
                WikidataId = match?.Wikidata ?? ""
            };
        }
    }
}