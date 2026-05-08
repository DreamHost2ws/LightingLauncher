using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AgLauncher.Core.GameManagement
{
    /// <summary>
    /// Manages game mods (Fabric, Forge, Quilt)
    /// </summary>
    public class ModManager
    {
        private readonly string _modsDirectory;
        private readonly HttpClient _httpClient;

        public enum ModLoader
        {
            Fabric,
            Forge,
            Quilt
        }

        public class Mod
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public ModLoader Loader { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public DateTime AddedDate { get; set; }
        }

        public ModManager(string modsDirectory = null)
        {
            _modsDirectory = modsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft", "mods");
            _httpClient = new HttpClient();

            if (!Directory.Exists(_modsDirectory))
                Directory.CreateDirectory(_modsDirectory);
        }

        /// <summary>
        /// Get all installed mods
        /// </summary>
        public List<Mod> GetInstalledMods()
        {
            var mods = new List<Mod>();

            if (Directory.Exists(_modsDirectory))
            {
                foreach (var file in Directory.GetFiles(_modsDirectory, "*.jar"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    mods.Add(new Mod
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = fileName,
                        Version = "1.0",
                        Loader = DetectModLoader(file),
                        AddedDate = File.GetCreationTime(file)
                    });
                }
            }

            return mods;
        }

        /// <summary>
        /// Install a mod from URL
        /// </summary>
        public async Task<bool> InstallModAsync(string modUrl, string modName)
        {
            try
            {
                var modPath = Path.Combine(_modsDirectory, $"{modName}.jar");
                
                using (var response = await _httpClient.GetAsync(modUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(modPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error installing mod: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Remove a mod
        /// </summary>
        public void RemoveMod(string modName)
        {
            var modPath = Path.Combine(_modsDirectory, $"{modName}.jar");
            if (File.Exists(modPath))
                File.Delete(modPath);
        }

        private ModLoader DetectModLoader(string filePath)
        {
            // Simplified detection - can be enhanced
            var fileName = Path.GetFileName(filePath).ToLower();
            
            if (fileName.Contains("fabric")) return ModLoader.Fabric;
            if (fileName.Contains("forge")) return ModLoader.Forge;
            if (fileName.Contains("quilt")) return ModLoader.Quilt;

            return ModLoader.Fabric; // Default
        }
    }
}
