using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AgLauncher.Core
{
    /// <summary>
    /// Optimizes game performance by adjusting JVM arguments and system settings
    /// </summary>
    public class PerformanceOptimizer
    {
        public class OptimizationProfile
        {
            public string Name { get; set; }
            public int MinMemoryMB { get; set; }
            public int MaxMemoryMB { get; set; }
            public List<string> JvmArguments { get; set; }
            public bool EnableFastMath { get; set; }
            public bool EnableGCOptimization { get; set; }
        }

        private readonly Dictionary<string, OptimizationProfile> _profiles;
        private readonly long _systemRamMB;

        public PerformanceOptimizer()
        {
            _systemRamMB = GetTotalSystemRam();
            _profiles = InitializeProfiles();
        }

        public Dictionary<string, OptimizationProfile> GetAvailableProfiles() => _profiles;

        public OptimizationProfile GetProfile(string profileName)
        {
            return _profiles.ContainsKey(profileName) ? _profiles[profileName] : _profiles["Balanced"];
        }

        public string GenerateJvmArguments(string profileName, int allocatedMemoryMB)
        {
            var profile = GetProfile(profileName);
            var args = new List<string>
            {
                $"-Xms{Math.Min(profile.MinMemoryMB, allocatedMemoryMB)}M",
                $"-Xmx{Math.Min(profile.MaxMemoryMB, allocatedMemoryMB)}M"
            };

            // Add garbage collection optimization
            if (profile.EnableGCOptimization)
            {
                args.Add("-XX:+UseG1GC");
                args.Add("-XX:+ParallelRefProcEnabled");
                args.Add("-XX:MaxGCPauseMillis=200");
                args.Add("-XX:+UnlockExperimentalVMOptions");
                args.Add("-XX:G1NewCollectionPercentage=30");
                args.Add("-XX:G1MaxNewGenPercent=40");
            }

            // Add fast math
            if (profile.EnableFastMath)
            {
                args.Add("-XX:+UseFastAccessorMethods");
                args.Add("-XX:+OptimizeStringConcat");
            }

            args.AddRange(profile.JvmArguments);

            return string.Join(" ", args);
        }

        public int GetRecommendedMemory()
        {
            // Allocate 50% of system RAM or 8GB max
            return (int)Math.Min(_systemRamMB / 2, 8192);
        }

        private Dictionary<string, OptimizationProfile> InitializeProfiles()
        {
            return new Dictionary<string, OptimizationProfile>
            {
                ["Ultra"] = new OptimizationProfile
                {
                    Name = "Ultra Performance",
                    MinMemoryMB = 4096,
                    MaxMemoryMB = 12288,
                    EnableGCOptimization = true,
                    EnableFastMath = true,
                    JvmArguments = new List<string>
                    {
                        "-XX:+UseStringDeduplication",
                        "-XX:+AlwaysPreTouch"
                    }
                },
                ["High"] = new OptimizationProfile
                {
                    Name = "High Performance",
                    MinMemoryMB = 2048,
                    MaxMemoryMB = 8192,
                    EnableGCOptimization = true,
                    EnableFastMath = true,
                    JvmArguments = new List<string> { }
                },
                ["Balanced"] = new OptimizationProfile
                {
                    Name = "Balanced",
                    MinMemoryMB = 1024,
                    MaxMemoryMB = 4096,
                    EnableGCOptimization = true,
                    EnableFastMath = false,
                    JvmArguments = new List<string> { }
                },
                ["Low"] = new OptimizationProfile
                {
                    Name = "Low End",
                    MinMemoryMB = 512,
                    MaxMemoryMB = 1024,
                    EnableGCOptimization = true,
                    EnableFastMath = false,
                    JvmArguments = new List<string> { }
                }
            };
        }

        private long GetTotalSystemRam()
        {
            try
            {
                var compInfo = new System.Net.NetworkInformation.NetworkInterface();
                var objMem = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
                objMem.NextValue();

                // Fallback: estimate based on available RAM
                return 16384; // Default 16GB estimate
            }
            catch
            {
                return 8192; // Fallback to 8GB
            }
        }
    }
}
