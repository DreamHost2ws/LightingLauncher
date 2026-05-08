using System;
using System.Threading.Tasks;
using AgLauncher.Core.Authentication;
using AgLauncher.Core.GameManagement;

namespace AgLauncher.Core
{
    /// <summary>
    /// Main launcher core coordinating all components
    /// </summary>
    public class LauncherCore
    {
        public CacheManager Cache { get; private set; }
        public PerformanceOptimizer Performance { get; private set; }
        public VersionManager Versions { get; private set; }
        public ModManager Mods { get; private set; }
        public GameLauncher GameLauncher { get; private set; }
        public OnlineAccountManager OnlineAccounts { get; private set; }
        public OfflineAccountManager OfflineAccounts { get; private set; }

        public LauncherCore()
        {
            Cache = new CacheManager();
            Performance = new PerformanceOptimizer();
            Versions = new VersionManager();
            Mods = new ModManager();
            GameLauncher = new GameLauncher();
            OnlineAccounts = new OnlineAccountManager();
            OfflineAccounts = new OfflineAccountManager();
        }

        /// <summary>
        /// Initialize launcher
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[LauncherCore] Initializing...");
                
                // Load cached data
                await Cache.GetAsync<object>("initialized");
                await Cache.SetAsync("initialized", true, TimeSpan.FromDays(1));

                System.Diagnostics.Debug.WriteLine("[LauncherCore] Initialization complete");
            }
            catch (Exception ex)
            {
                throw new Exception($"Launcher initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Shutdown launcher and cleanup
        /// </summary>
        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("[LauncherCore] Shutting down...");
            // Cleanup resources
        }
    }
}
