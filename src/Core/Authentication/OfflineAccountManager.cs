using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace AgLauncher.Core.Authentication
{
    /// <summary>
    /// Manages offline local Minecraft accounts
    /// </summary>
    public class OfflineAccountManager
    {
        private readonly string _accountsDirectory;

        public class OfflineProfile
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastPlayed { get; set; }
        }

        public OfflineAccountManager(string accountsDirectory = null)
        {
            _accountsDirectory = accountsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ag-Lancher", "Accounts");

            if (!Directory.Exists(_accountsDirectory))
                Directory.CreateDirectory(_accountsDirectory);
        }

        /// <summary>
        /// Create a new offline account
        /// </summary>
        public OfflineProfile CreateAccount(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty");

            if (username.Length > 16)
                throw new ArgumentException("Username cannot exceed 16 characters");

            var profile = new OfflineProfile
            {
                Id = GenerateUUID(username),
                Username = username,
                CreatedAt = DateTime.UtcNow,
                LastPlayed = DateTime.UtcNow
            };

            SaveProfile(profile);
            return profile;
        }

        /// <summary>
        /// Get all offline accounts
        /// </summary>
        public List<OfflineProfile> GetAllAccounts()
        {
            var accounts = new List<OfflineProfile>();

            if (Directory.Exists(_accountsDirectory))
            {
                foreach (var file in Directory.GetFiles(_accountsDirectory, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var profile = JsonConvert.DeserializeObject<OfflineProfile>(json);
                        if (profile != null)
                            accounts.Add(profile);
                    }
                    catch { }
                }
            }

            return accounts;
        }

        /// <summary>
        /// Get account by username
        /// </summary>
        public OfflineProfile GetAccount(string username)
        {
            var accounts = GetAllAccounts();
            return accounts.Find(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Delete offline account
        /// </summary>
        public void DeleteAccount(string username)
        {
            var filePath = Path.Combine(_accountsDirectory, $"{username}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private void SaveProfile(OfflineProfile profile)
        {
            var filePath = Path.Combine(_accountsDirectory, $"{profile.Username}.json");
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private string GenerateUUID(string username)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"OfflineMode:{username}"));
                var uuid = new StringBuilder();
                uuid.Append(BitConverter.ToString(hash).Replace("-", "").ToLower());
                return uuid.ToString();
            }
        }
    }
}
