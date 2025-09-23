using Newtonsoft.Json.Linq;

namespace Website.App
{
    public class Settings
    {
        private readonly static string settingsFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "AppSettings.json");
        private static string? logpath;
        private static string? filenamepattern;
        private static string? mapboxtoken;
        private static string? databasepath;
        private static Dictionary<string, string>? connectionstrings;
        public static string LogPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(logpath)) Load();
                return logpath ?? settingsFilePath;
            }
            set
            {
                logpath = value;
                Save();
            }
        }
        public static string FileNamePattern
        {
            get
            {
                if (string.IsNullOrWhiteSpace(filenamepattern)) Load();
                return filenamepattern ?? "Website.App.{0:dd-MM-yyyy}.log";
            }
            set
            {
                filenamepattern = value;
                Save();
            }
        }
        public static string MapboxToken
        {
            get
            {
                if (string.IsNullOrWhiteSpace(mapboxtoken)) Load();
                return mapboxtoken ?? string.Empty;
            }
            set
            {
                mapboxtoken = value;
                Save();
            }
        }
        public static Dictionary<string, string> ConnectionStrings
        {
            get
            {
                if (connectionstrings == null)
                {
                    connectionstrings = [];
                    Load();
                }
                return connectionstrings;
            }
            set
            {
                connectionstrings = value ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Save();
            }
        }
        public static string DefaultConnectionString
        {
            get
            {
                if (ConnectionStrings == null || ConnectionStrings.Count == 0) Load();
                return ConnectionStrings?.First().Value ?? string.Empty;
            }
        }
        public static string DatabasePath
        {
            get
            {
                if (databasepath == null || ConnectionStrings.Count == 0) Load();
                return databasepath ?? string.Empty;
            }
        }

        public static void Load()
        {
            if (!File.Exists(settingsFilePath)) throw new FileNotFoundException("Config file not found", settingsFilePath);

            var json = File.ReadAllText(settingsFilePath);
            var root = JObject.Parse(json);

            foreach (var prop in root.Properties())
            {
                switch (prop.Name)
                {
                    case "LogPath":
                        logpath = prop.Value?.ToString() ?? "";
                        break;

                    case "FileNamePattern":
                        // "App.{0:dd-MM-yyyy}.log"

                        filenamepattern = string.Format(prop.Value?.ToString() ?? "", DateTime.Now);
                        break;

                    case "MapboxToken":
                        mapboxtoken = prop.Value?.ToString() ?? "";
                        break;

                    case "DatabasePath":
                        databasepath = prop.Value?.ToString() ?? string.Empty;
                        break;

                    case "ConnectionStrings":
                        if (prop.Value is JObject csObj)
                        {
                            connectionstrings = [];
                            foreach (var kv in csObj.Properties())
                            {
                                connectionstrings[kv.Name] = kv.Value?.ToString() ?? "";
                            }

                            // optional: do something with connectionstrings here
                        }
                        break;
                }
            }
        }
        private static void Save()
        {
            var root = new JObject
            {
                [nameof(LogPath)] = logpath ?? "",
                [nameof(FileNamePattern)] = filenamepattern ?? "",
                [nameof(MapboxToken)] = mapboxtoken ?? "",
                [nameof(DatabasePath)] = DatabasePath ?? "",
                [nameof(ConnectionStrings)] = new JObject(
                    connectionstrings?.Select(kvp => new JProperty(kvp.Key, kvp.Value)) ?? []
                )
            };

            File.WriteAllText(settingsFilePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
