using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AgLauncher.Core;

namespace AgLauncher.JAR
{
    /// <summary>
    /// Provides compatibility layer for other Minecraft launchers
    /// </summary>
    public class JARCompatibility
    {
        private readonly LauncherCore _launcherCore;
        private readonly string _exportDirectory;

        public JARCompatibility(string exportDirectory = null)
        {
            _launcherCore = new LauncherCore();
            _exportDirectory = exportDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ag-Lancher", "JAR");

            if (!Directory.Exists(_exportDirectory))
                Directory.CreateDirectory(_exportDirectory);
        }

        /// <summary>
        /// Export cache data in standard format
        /// </summary>
        public async Task<string> ExportCacheAsync(string exportPath)
        {
            try
            {
                var cacheExportDir = Path.Combine(exportPath, "cache_export");
                if (Directory.Exists(cacheExportDir))
                    Directory.Delete(cacheExportDir, true);
                Directory.CreateDirectory(cacheExportDir);

                // Export version information
                var versions = _launcherCore.Versions.GetInstalledVersions();
                var versionJson = Newtonsoft.Json.JsonConvert.SerializeObject(versions);
                await File.WriteAllTextAsync(
                    Path.Combine(cacheExportDir, "versions.json"), versionJson);

                // Export mod information
                var mods = _launcherCore.Mods.GetInstalledMods();
                var modsJson = Newtonsoft.Json.JsonConvert.SerializeObject(mods);
                await File.WriteAllTextAsync(
                    Path.Combine(cacheExportDir, "mods.json"), modsJson);

                return cacheExportDir;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cache export failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Import cache data from other launchers
        /// </summary>
        public async Task<bool> ImportCacheAsync(string importPath)
        {
            try
            {
                if (!Directory.Exists(importPath))
                    throw new DirectoryNotFoundException($"Import path not found: {importPath}");

                // Import versions
                var versionsFile = Path.Combine(importPath, "versions.json");
                if (File.Exists(versionsFile))
                {
                    var json = await File.ReadAllTextAsync(versionsFile);
                    var versions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
                    System.Diagnostics.Debug.WriteLine($"Imported {versions?.Count ?? 0} versions");
                }

                // Import mods
                var modsFile = Path.Combine(importPath, "mods.json");
                if (File.Exists(modsFile))
                {
                    var json = await File.ReadAllTextAsync(modsFile);
                    System.Diagnostics.Debug.WriteLine("Imported mods data");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cache import failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get launcher information for compatibility
        /// </summary>
        public Dictionary<string, string> GetLauncherInfo()
        {
            return new Dictionary<string, string>
            {
                ["Name"] = "Ag-Lancher",
                ["Version"] = "1.0.0",
                ["API"] = "JAR",
                ["Platform"] = "Windows, macOS, Linux, Android, iOS",
                ["Features"] = "Cache, Optimization, Multi-Account, Mods"
            };
        }
    }
}
