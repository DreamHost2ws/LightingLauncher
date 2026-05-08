using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AgLauncher.Core
{
    /// <summary>
    /// Manages caching of game assets, versions, and mod data
    /// </summary>
    public class CacheManager
    {
        private readonly string _cacheDirectory;
        private readonly Dictionary<string, CacheEntry> _memoryCache;
        private readonly long _maxCacheSize; // in bytes
        private long _currentCacheSize;

        public CacheManager(string cacheDirectory = null, long maxCacheSizeMB = 5000)
        {
            _cacheDirectory = cacheDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ag-Lancher", "Cache");
            _maxCacheSize = maxCacheSizeMB * 1024 * 1024;
            _memoryCache = new Dictionary<string, CacheEntry>();
            _currentCacheSize = 0;

            InitializeCache();
        }

        private void InitializeCache()
        {
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }

        /// <summary>
        /// Get cached item by key
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow <= entry.ExpirationTime)
                {
                    return entry.Data as T;
                }
                else
                {
                    _memoryCache.Remove(key);
                }
            }

            // Check file cache
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var cached = JsonConvert.DeserializeObject<CachedObject<T>>(json);
                    
                    if (cached != null && DateTime.UtcNow <= cached.ExpirationTime)
                    {
                        return cached.Data;
                    }
                    else
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cache read error: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Set cache item with optional expiration
        /// </summary>
        public async Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class
        {
            var expirationTime = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(24));
            var cacheEntry = new CacheEntry { Data = data, ExpirationTime = expirationTime };

            // Add to memory cache
            _memoryCache[key] = cacheEntry;

            // Add to file cache
            try
            {
                var filePath = GetCacheFilePath(key);
                var cached = new CachedObject<T> { Data = data, ExpirationTime = expirationTime };
                var json = JsonConvert.SerializeObject(cached);
                
                await File.WriteAllTextAsync(filePath, json);
                _currentCacheSize += json.Length;

                // Cleanup if cache exceeded
                if (_currentCacheSize > _maxCacheSize)
                    await CleanupCacheAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache write error: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove cache item
        /// </summary>
        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        public void Clear()
        {
            _memoryCache.Clear();
            if (Directory.Exists(_cacheDirectory))
                Directory.Delete(_cacheDirectory, true);
            InitializeCache();
            _currentCacheSize = 0;
        }

        /// <summary>
        /// Cleanup expired cache entries
        /// </summary>
        private async Task CleanupCacheAsync()
        {
            var now = DateTime.UtcNow;
            var cacheDir = new DirectoryInfo(_cacheDirectory);
            
            var expiredFiles = cacheDir.GetFiles("*.json")
                .Where(f => f.CreationTimeUtc.AddHours(24) < now)
                .ToList();

            foreach (var file in expiredFiles)
            {
                try
                {
                    _currentCacheSize -= file.Length;
                    file.Delete();
                }
                catch { }
            }

            await Task.CompletedTask;
        }

        private string GetCacheFilePath(string key)
        {
            var fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                .Replace("/", "_").Replace("+", "-") + ".json";
            return Path.Combine(_cacheDirectory, fileName);
        }

        private class CacheEntry
        {
            public object Data { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private class CachedObject<T> where T : class
        {
            public T Data { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
    }
}
