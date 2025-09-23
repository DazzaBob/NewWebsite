using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V6
{
    internal class ResolvedAddressFeature
    {
        internal string FeatureId { get; set; } = "";
        internal string Name { get; set; } = "";
        internal GeoCoordinates Coords { get; set; } = new();      // feature-level geometry point (usually address centroid)
        internal BoundingBox? BBox { get; set; }                   // feature-level bbox (if present)
        internal AddressBlock? Address { get; set; }               // optional — present for address/street features
        internal PlaceBlock? Place { get; set; }                   // optional — present if context.place exists
        internal PlaceBlock? Locality { get; set; }                // optional — present if context.locality exists
        internal RegionBlock? Region { get; set; }                 // optional
        internal CountryBlock? Country { get; set; }               // optional

        internal class GeoCoordinates
        {
            internal double? DisplayLatitude { get; set; }
            internal double? DisplayLongitude { get; set; }
            internal double? RoutableLatitude { get; set; }
            internal double? RoutableLongitude { get; set; }
        }

        internal class BoundingBox
        {
            internal double MinLongitude { get; set; }
            internal double MinLatitude { get; set; }
            internal double MaxLongitude { get; set; }
            internal double MaxLatitude { get; set; }
        }

        internal class AddressBlock
        {
            internal string? Name { get; set; }
            internal string? Number { get; set; }
            internal string? FullAddress { get; set; }
            internal PostcodeBlock? Postcode { get; set; }
            internal GeoCoordinates Coords { get; set; } = new(); // display + routable for the address
        }

        internal class PostcodeBlock
        {
            internal string? Name { get; set; }
        }

        internal class PlaceBlock
        {
            internal string? Name { get; set; }
            internal GeoCoordinates? Coords { get; set; }     // centroid for place/locality (nullable if missing)
            internal BoundingBox? BBox { get; set; }         // bbox for place/locality (nullable if missing)
        }

        internal class RegionBlock
        {
            internal string? Name { get; set; }
            internal string? RegionCode { get; set; }
            internal string? RegionCodeFull { get; set; }
        }

        internal class CountryBlock
        {
            internal string? Name { get; set; }
            internal string? CountryCode { get; set; }
            internal string? CountryCodeAlpha3 { get; set; }
        }

        internal static ResolvedAddressFeature Parse(JsonElement feature)
        {
            var result = new ResolvedAddressFeature
            {
                FeatureId = feature.GetProperty("id").GetString() ?? "",
                Name = (feature.GetProperty("properties").TryGetProperty("name_preferred", out var np) && np.ValueKind == JsonValueKind.String)
                       ? np.GetString()!
                       : (feature.GetProperty("properties").TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                         ? n.GetString()!
                         : ""
            };

            // Geometry (feature-level)
            double? geomLon = null, geomLat = null;
            if (feature.TryGetProperty("geometry", out var geometry) &&
                geometry.TryGetProperty("coordinates", out var coords) &&
                coords.ValueKind == JsonValueKind.Array && coords.GetArrayLength() == 2)
            {
                geomLon = coords[0].GetDouble();
                geomLat = coords[1].GetDouble();
                result.Coords.DisplayLongitude = geomLon;
                result.Coords.DisplayLatitude = geomLat;
            }

            // Properties
            if (feature.TryGetProperty("properties", out var props))
            {
                // feature-level bbox (may be place/locality bbox if feature is that type)
                if (props.TryGetProperty("bbox", out var bbox) && bbox.ValueKind == JsonValueKind.Array && bbox.GetArrayLength() == 4)
                {
                    result.BBox = new BoundingBox
                    {
                        MinLongitude = bbox[0].GetDouble(),
                        MinLatitude = bbox[1].GetDouble(),
                        MaxLongitude = bbox[2].GetDouble(),
                        MaxLatitude = bbox[3].GetDouble()
                    };
                }

                // Address block (initialize; may remain empty if no address in context)
                var addressBlock = new AddressBlock
                {
                    FullAddress = props.TryGetProperty("full_address", out var fullAddr) && fullAddr.ValueKind == JsonValueKind.String
                                  ? fullAddr.GetString()
                                  : null
                };

                // props.coordinates may contain routable coordinates and/or direct lon/lat
                if (props.TryGetProperty("coordinates", out var propsCoords) && propsCoords.ValueKind == JsonValueKind.Object)
                {
                    if (propsCoords.TryGetProperty("longitude", out var pLon) && pLon.ValueKind != JsonValueKind.Null)
                        addressBlock.Coords.RoutableLongitude = pLon.GetDouble();
                    if (propsCoords.TryGetProperty("latitude", out var pLat) && pLat.ValueKind != JsonValueKind.Null)
                        addressBlock.Coords.RoutableLatitude = pLat.GetDouble();

                    // routable_points array overrides coordinates.routable_points if present — use first routable point
                    if (propsCoords.TryGetProperty("routable_points", out var rpoints) &&
                        rpoints.ValueKind == JsonValueKind.Array && rpoints.GetArrayLength() > 0)
                    {
                        var firstPoint = rpoints[0];
                        if (firstPoint.TryGetProperty("longitude", out var rpLon) && rpLon.ValueKind != JsonValueKind.Null)
                            addressBlock.Coords.RoutableLongitude = rpLon.GetDouble();
                        if (firstPoint.TryGetProperty("latitude", out var rpLat) && rpLat.ValueKind != JsonValueKind.Null)
                            addressBlock.Coords.RoutableLatitude = rpLat.GetDouble();
                    }
                }

                // If geometry gives a point and there's an address context, that geometry is the address display centroid
                // (For place/locality features, geometry refers to that feature instead)
                if (geomLon.HasValue && geomLat.HasValue)
                {
                    // we'll assign display coords to address only when address context exists later;
                    // keep them in local vars and set on addressBlock if we detect address context
                }

                // Context parsing
                if (props.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.Object)
                {
                    // Address context (street number)
                    if (context.TryGetProperty("address", out var ctxAddress) && ctxAddress.ValueKind == JsonValueKind.Object)
                    {
                        if (ctxAddress.TryGetProperty("address_number", out var addrNum) && addrNum.ValueKind == JsonValueKind.String)
                            addressBlock.Number = addrNum.GetString();
                    }

                    // Street name (two possible locations)
                    if (context.TryGetProperty("street", out var ctxStreet) && ctxStreet.TryGetProperty("name", out var streetName) && streetName.ValueKind == JsonValueKind.String)
                    {
                        addressBlock.Name = streetName.GetString();
                    }
                    else if (context.TryGetProperty("address", out var ctxAddress2) && ctxAddress2.TryGetProperty("street_name", out var streetName2) && streetName2.ValueKind == JsonValueKind.String)
                    {
                        addressBlock.Name = streetName2.GetString();
                    }

                    // Postcode
                    addressBlock.Postcode = context.TryGetProperty("postcode", out var pc) && pc.TryGetProperty("name", out var pcName) && pcName.ValueKind == JsonValueKind.String
                        ? new PostcodeBlock { Name = pcName.GetString() }
                        : null;

                    // Place
                    if (context.TryGetProperty("place", out var ctxPlace) && ctxPlace.TryGetProperty("name", out var ctxPlaceName) && ctxPlaceName.ValueKind == JsonValueKind.String)
                    {
                        var place = new PlaceBlock { Name = ctxPlaceName.GetString() };

                        // If this feature itself is a place (feature_type == "place"), geometry is the place centroid and props.bbox is place bbox
                        if (props.TryGetProperty("feature_type", out var ftPlace) && ftPlace.ValueKind == JsonValueKind.String && ftPlace.GetString() == "place")
                        {
                            if (geomLon.HasValue && geomLat.HasValue)
                                place.Coords = new GeoCoordinates { DisplayLongitude = geomLon, DisplayLatitude = geomLat };

                            if (result.BBox != null)
                                place.BBox = result.BBox;
                        }
                        else
                        {
                            // sometimes place centroid/bbox are available in props.coordinates / props.bbox
                            if (props.TryGetProperty("coordinates", out var placeCoords) && placeCoords.ValueKind == JsonValueKind.Object
                                && placeCoords.TryGetProperty("longitude", out var pLon2) && pLon2.ValueKind != JsonValueKind.Null
                                && placeCoords.TryGetProperty("latitude", out var pLat2) && pLat2.ValueKind != JsonValueKind.Null)
                            {
                                place.Coords = new GeoCoordinates
                                {
                                    DisplayLongitude = pLon2.GetDouble(),
                                    DisplayLatitude = pLat2.GetDouble()
                                };
                            }
                            else if (result.BBox != null)
                            {
                                // if only bbox is present it might be for place — accept it
                                place.BBox = result.BBox;
                            }
                        }

                        result.Place = place;
                    }

                    // Locality
                    if (context.TryGetProperty("locality", out var ctxLocality) && ctxLocality.TryGetProperty("name", out var ctxLocalityName) && ctxLocalityName.ValueKind == JsonValueKind.String)
                    {
                        var locality = new PlaceBlock { Name = ctxLocalityName.GetString() };

                        if (props.TryGetProperty("feature_type", out var ftLocal) && ftLocal.ValueKind == JsonValueKind.String && ftLocal.GetString() == "locality")
                        {
                            if (geomLon.HasValue && geomLat.HasValue)
                                locality.Coords = new GeoCoordinates { DisplayLongitude = geomLon, DisplayLatitude = geomLat };

                            if (result.BBox != null)
                                locality.BBox = result.BBox;
                        }
                        else
                        {
                            if (props.TryGetProperty("coordinates", out var locCoords) && locCoords.ValueKind == JsonValueKind.Object
                                && locCoords.TryGetProperty("longitude", out var lLon) && lLon.ValueKind != JsonValueKind.Null
                                && locCoords.TryGetProperty("latitude", out var lLat) && lLat.ValueKind != JsonValueKind.Null)
                            {
                                locality.Coords = new GeoCoordinates
                                {
                                    DisplayLongitude = lLon.GetDouble(),
                                    DisplayLatitude = lLat.GetDouble()
                                };
                            }
                            else if (result.BBox != null)
                            {
                                locality.BBox = result.BBox;
                            }
                        }

                        result.Locality = locality;
                    }

                    // Region
                    if (context.TryGetProperty("region", out var ctxRegion) && ctxRegion.TryGetProperty("name", out var ctxRegionName) && ctxRegionName.ValueKind == JsonValueKind.String)
                    {
                        result.Region = new RegionBlock
                        {
                            Name = ctxRegionName.GetString(),
                            RegionCode = ctxRegion.TryGetProperty("region_code", out var ctxRegCode) && ctxRegCode.ValueKind == JsonValueKind.String ? ctxRegCode.GetString() : null,
                            RegionCodeFull = ctxRegion.TryGetProperty("region_code_full", out var ctxRegCodeFull) && ctxRegCodeFull.ValueKind == JsonValueKind.String ? ctxRegCodeFull.GetString() : null
                        };
                    }

                    // Country
                    if (context.TryGetProperty("country", out var ctxCountry) && ctxCountry.TryGetProperty("name", out var ctxCountryName) && ctxCountryName.ValueKind == JsonValueKind.String)
                    {
                        result.Country = new CountryBlock
                        {
                            Name = ctxCountryName.GetString(),
                            CountryCode = ctxCountry.TryGetProperty("country_code", out var ctxCCode) && ctxCCode.ValueKind == JsonValueKind.String ? ctxCCode.GetString() : null,
                            CountryCodeAlpha3 = ctxCountry.TryGetProperty("country_code_alpha_3", out var ctxCCode3) && ctxCCode3.ValueKind == JsonValueKind.String ? ctxCCode3.GetString() : null
                        };
                    }

                    // If address context existed (we found number or name), assign the geometry as display coords for address
                    if ((addressBlock.Number != null || addressBlock.Name != null || addressBlock.FullAddress != null) && geomLon.HasValue && geomLat.HasValue)
                    {
                        addressBlock.Coords.DisplayLongitude = geomLon;
                        addressBlock.Coords.DisplayLatitude = geomLat;
                    }
                } // end context

                result.Address = addressBlock;
            } // end props

            return result;
        }
    }
}