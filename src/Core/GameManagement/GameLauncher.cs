using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgLauncher.Core.Authentication;

namespace AgLauncher.Core.GameManagement
{
    /// <summary>
    /// Launches Minecraft game with optimized settings
    /// </summary>
    public class GameLauncher
    {
        private readonly PerformanceOptimizer _performanceOptimizer;
        private readonly string _javaPath;

        public GameLauncher(string javaPath = null)
        {
            _performanceOptimizer = new PerformanceOptimizer();
            _javaPath = javaPath ?? FindJavaPath();
        }

        /// <summary>
        /// Launch game with specified parameters
        /// </summary>
        public async Task<bool> LaunchGameAsync(
            string version,
            string username,
            string userId,
            string sessionToken,
            string optimizationProfile = "Balanced",
            int memoryAllocationMB = 0)
        {
            try
            {
                if (!File.Exists(_javaPath))
                    throw new FileNotFoundException($"Java not found at {_javaPath}");

                var minecraftPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".minecraft");

                var versionPath = Path.Combine(minecraftPath, "versions", version);
                if (!Directory.Exists(versionPath))
                    throw new DirectoryNotFoundException($"Version {version} not found");

                if (memoryAllocationMB <= 0)
                    memoryAllocationMB = _performanceOptimizer.GetRecommendedMemory();

                var jvmArgs = _performanceOptimizer.GenerateJvmArguments(optimizationProfile, memoryAllocationMB);
                var jarPath = Path.Combine(versionPath, $"{version}.jar");

                var gameArgs = new List<string>
                {
                    "--username", username,
                    "--uuid", userId,
                    "--session", sessionToken,
                    "--gameDir", minecraftPath,
                    "--assetsDir", Path.Combine(minecraftPath, "assets"),
                    "--assetIndex", version
                };

                var args = $"{jvmArgs} -cp \"{jarPath}\" net.minecraft.client.main.Main {string.Join(" ", gameArgs)}";

                var processInfo = new ProcessStartInfo
                {
                    FileName = _javaPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                using (var process = Process.Start(processInfo))
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Game launch error: {ex.Message}", ex);
            }
        }

        private string FindJavaPath()
        {
            var commonPaths = new[]
            {
                @"C:\Program Files\Java\jdk-17\bin\java.exe",
                @"C:\Program Files (x86)\Java\jdk-17\bin\java.exe",
                @"/usr/bin/java",
                @"/usr/local/bin/java",
                @"/Library/Java/JavaVirtualMachines/jdk-17/Contents/Home/bin/java"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException("Java installation not found. Please install Java 8 or higher.");
        }
    }
}
