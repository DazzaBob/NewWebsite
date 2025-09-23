namespace Website.App
{
    public class Bootstrap
    {
        private static Helper.Logger? _logger;
        private static bool _isInitialized = false;
        internal static object ONGMutex = new(); // The mutex for the OrderNumberGenerator

        static Bootstrap()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("✅ DEBUG build active.");
#else
    System.Diagnostics.Debug.WriteLine("🚫 RELEASE build active.");
#endif

            if (!_isInitialized)
            {
                _logger = new App.Helper.Logger(Settings.LogPath + "\\" + Settings.FileNamePattern);

                ONGMutex = new object(); // Initialize the mutex for the OrderNumberGenerator

                // Start the zoning operations, incase we crashed during zoning.
                // No background zoning, just start the zone manager and let it handle the zoning operations.
                App.Operations.Zoning.ZoneManager.StartZoning(false);
                _isInitialized = true;
            }
        }
        public static App.Helper.Logger? Logger
        {
            get => _logger;
            set => _logger = value;
        }
        internal static void Dispose()
        {
            _isInitialized = false;
            _logger?.Dispose();
            GC.Collect();
        }
    }
}
