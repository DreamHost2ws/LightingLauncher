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
    /// Manages Minecraft version installation and management
    /// </summary>
    public class VersionManager
    {
        private const string MANIFEST_URL = "https://launcher.mojang.com/v1/objects/1a2345678901234567890123456789ab/version_manifest.json";
        private readonly string _versionsDirectory;
        private readonly HttpClient _httpClient;

        public class Version
        {
            public string Id { get; set; }
            public string Type { get; set; } // release, snapshot, old_alpha, old_beta
            public string Url { get; set; }
            public DateTime ReleaseTime { get; set; }
            public bool IsInstalled { get; set; }
        }

        public VersionManager(string versionsDirectory = null)
        {
            _versionsDirectory = versionsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft", "versions");
            _httpClient = new HttpClient();

            if (!Directory.Exists(_versionsDirectory))
                Directory.CreateDirectory(_versionsDirectory);
        }

        /// <summary>
        /// Get all available versions
        /// </summary>
        public async Task<List<Version>> GetAvailableVersionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(MANIFEST_URL);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic manifest = JsonConvert.DeserializeObject(json);
                    var versions = new List<Version>();

                    foreach (var version in manifest["versions"])
                    {
                        versions.Add(new Version
                        {
                            Id = version["id"],
                            Type = version["type"],
                            Url = version["url"],
                            ReleaseTime = DateTime.Parse(version["releaseTime"].ToString()),
                            IsInstalled = IsVersionInstalled(version["id"].ToString())
                        });
                    }

                    return versions.OrderByDescending(v => v.ReleaseTime).ToList();
                }

                throw new Exception("Failed to fetch version manifest");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting versions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Install a specific version
        /// </summary>
        public async Task<bool> InstallVersionAsync(string versionId, IProgress<double> progress = null)
        {
            try
            {
                var versionDir = Path.Combine(_versionsDirectory, versionId);
                if (Directory.Exists(versionDir))
                    return true; // Already installed

                Directory.CreateDirectory(versionDir);

                // Download version JSON
                var versionJsonUrl = $"https://launcher.mojang.com/v1/objects/1a2345678901234567890123456789ab/versions/{versionId}/{versionId}.json";
                var versionJson = await _httpClient.GetStringAsync(versionJsonUrl);
                var versionJsonPath = Path.Combine(versionDir, $"{versionId}.json");
                await File.WriteAllTextAsync(versionJsonPath, versionJson);

                // Download version JAR
                var jarUrl = $"https://launcher.mojang.com/v1/objects/1a2345678901234567890123456789ab/versions/{versionId}/{versionId}.jar";
                var jarPath = Path.Combine(versionDir, $"{versionId}.jar");
                
                using (var response = await _httpClient.GetAsync(jarUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(jarPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            downloadedBytes = totalRead;
                            
                            if (totalBytes > 0)
                                progress?.Report((double)downloadedBytes / totalBytes);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error installing version: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get installed versions
        /// </summary>
        public List<string> GetInstalledVersions()
        {
            var versions = new List<string>();

            if (Directory.Exists(_versionsDirectory))
            {
                foreach (var dir in Directory.GetDirectories(_versionsDirectory))
                {
                    var dirName = Path.GetFileName(dir);
                    var jarFile = Path.Combine(dir, $"{dirName}.jar");
                    
                    if (File.Exists(jarFile))
                        versions.Add(dirName);
                }
            }

            return versions.OrderByDescending(v => v).ToList();
        }

        private bool IsVersionInstalled(string versionId)
        {
            return GetInstalledVersions().Contains(versionId);
        }
    }
}
