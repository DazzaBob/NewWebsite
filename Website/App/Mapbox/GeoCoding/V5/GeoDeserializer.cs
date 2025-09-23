using System.Text.Json;

namespace Website.App.Mapbox.GeoCoding.V5
{
    internal static class GeoDeserializer
    {
        private static readonly string NamespaceClass = "App.Mapbox.GeoCoding.V5.GeoDeserializer.";
        private static string errorMessage = string.Empty;

        /// <summary>
        /// Parses a raw Mapbox JSON response string and extracts the first usable feature as a <see cref="ResolvedAddressFeature"/>.
        /// This method supports both standard FeatureCollection responses (with a "features" array)
        /// and single Feature objects (with "type": "Feature") as returned in some Mapbox variants.
        ///
        /// All deserialization steps are wrapped in error handling with contextual logging via <c>Bootstrap.Logger</c>.
        /// This ensures safe failure when the payload is malformed or unexpectedly empty.
        ///
        /// Supported JSON structures:
        /// - {"type":"FeatureCollection", "features":[...]} — standard Mapbox format
        /// - {"type":"Feature", ...} — alternate/simplified Mapbox format
        ///
        /// <para><b>Logging & Diagnostics:</b></para>
        /// - Logs specific errors for missing payload, invalid structure, and deserialization issues.
        /// - Prepends all log messages with a consistent method prefix for tracing.
        ///
        /// <para><b>Throws:</b></para>
        /// <list type="bullet">
        /// <item><see cref="ArgumentException"/> if the JSON is null or whitespace.</item>
        /// <item><see cref="InvalidOperationException"/> if no valid feature is found or deserialization fails.</item>
        /// <item><see cref="JsonException"/> if JSON parsing fails due to structural issues.</item>
        /// </list>
        ///
        /// <param name="json">The raw JSON response string from Mapbox.</param>
        /// <returns>A <see cref="ResolvedAddressFeature"/> parsed from the best-matching feature.</returns>
        internal static async Task<Website.App.Mapbox.GeoCoding.V5.FeatureParser.ResolvedAddressFeature> ParseFromJsonAsync(string json)
        {
            const string method = "ParseFromJsonAsync: ";
            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = NamespaceClass + method + "JSON payload is empty.";
                Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                throw new ArgumentException(errorMessage, nameof(json));
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = await Task.Run(() => doc.RootElement);

                if (doc.RootElement.TryGetProperty("type", out var t) && t.GetString() == "Feature")
                {
                    Website.App.Mapbox.GeoCoding.V5.FeatureParser.Feature? featureObj = await Task.Run(() => JsonSerializer.Deserialize<Website.App.Mapbox.GeoCoding.V5.FeatureParser.Feature>(json));
                    if (featureObj == null)
                    {
                        errorMessage = NamespaceClass + method + "Could not deserialize single feature.";
                        Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                        throw new InvalidOperationException("");
                    }
                    Website.App.Mapbox.GeoCoding.V5.FeatureParser.ResolvedAddressFeature RAF = await Task.Run(() => Website.App.Mapbox.GeoCoding.V5.FeatureParser.ParseResolvedFeature(featureObj));

                    string finalPath = DiskCache.GetPathFromParsedFeature(RAF);
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
                    await File.WriteAllTextAsync(finalPath, json);

                    return RAF;
                }

                if (root.TryGetProperty("features", out var featuresArray) && featuresArray.GetArrayLength() > 0)
                {
                    var featureObj = JsonSerializer.Deserialize<FeatureParser.Feature>(featuresArray[0].GetRawText());
                    if (featureObj == null)
                    {
                        errorMessage = NamespaceClass + method + "Could not deserialize feature.";
                        Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                        throw new InvalidOperationException("");
                    }
                    return FeatureParser.ParseResolvedFeature(featureObj);
                }

                errorMessage = NamespaceClass + method + "No valid feature or features array found in JSON.";
                Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                throw new InvalidOperationException(errorMessage);
            }
            catch (JsonException ex)
            {
                errorMessage = NamespaceClass + method + "No valid feature or features array found in JSON.";
                Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                throw new InvalidOperationException("Failed to parse Mapbox response JSON.", ex);
            }
        }

        internal static async Task<string> FetchGeoJsonAsync(string placeQuery)
        {
            if (string.IsNullOrWhiteSpace(placeQuery))
            {
                string errorMessage = $"App.Mapbox.GeoCoding.V5.GeoDeserializer.FetchGeoJsonAsync Query is required, {nameof(placeQuery)}";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                throw new ArgumentException(errorMessage);
            }

            // TEMP: Use raw query path just to fetch/locate the file
            string tempPath = DiskCache.GetTemporaryPathFromQuery(placeQuery);
            if (File.Exists(tempPath))
            {
                return await File.ReadAllTextAsync(tempPath);
            }

            using var client = new HttpClient();
            var encodedQuery = Uri.EscapeDataString(placeQuery);
            var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{encodedQuery}.json?access_token={Settings.MapboxToken}&limit=1";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Parse to get canonical cache path
            var parsed = await ParseFromJsonAsync(json);
            string finalPath = DiskCache.GetPathFromParsedFeature(parsed);

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            await File.WriteAllTextAsync(finalPath, json);

            return json;
        }

        private static class DiskCache
        {
            private static readonly string BaseCacheDir = Path.Combine(Settings.LogPath, "mapbox_cache");

            internal static string GetTemporaryPathFromQuery(string query)
            {
                string safe = Sanitize(query);
                return Path.Combine(BaseCacheDir, "temp", $"{safe}.json");
            }

            internal static string GetPathFromParsedFeature(Website.App.Mapbox.GeoCoding.V5.FeatureParser.ResolvedAddressFeature f)
            {
                string country = Sanitize(f.Country.Name ?? "xx");
                string region = Sanitize(f.Region.Name ?? "unknown-region");
                string place = Sanitize(f.Place.Name ?? f.Locality.Name ?? "unknown-place");

                if (!string.IsNullOrEmpty(f.FullAddress))
                {
                    // Full address (number + street + place)
                    string street = Sanitize(f.StreetName);
                    string number = Sanitize(f.StreetNumber);
                    return Path.Combine(BaseCacheDir, country, region, place, street, $"{number}.json");
                }

                // Fallback to place-level caching
                return Path.Combine(BaseCacheDir, country, region, place, "place.json");
            }

            private static string Sanitize(string input)
            {
                System.Text.StringBuilder builder = new();
                foreach (char c in input.ToLowerInvariant())
                {
                    if (char.IsLetterOrDigit(c)) builder.Append(c);
                    else if (char.IsWhiteSpace(c) || c == '/') builder.Append('-');
                }

                var sanitized = builder.ToString();
                while (sanitized.Contains("--"))
                    sanitized = sanitized.Replace("--", "-");

                return sanitized.Trim('-');
            }
        }
    }
}
