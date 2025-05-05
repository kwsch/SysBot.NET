using Newtonsoft.Json;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.WinForms
{
    public class UpdateChecker
    {
        private const string RepositoryOwner = "Havokx89";
        private const string RepositoryName = "DudeBot.NET";

        public static async Task<(bool UpdateAvailable, bool UpdateRequired, string NewVersion)> CheckForUpdatesAsync(bool forceShow = false)
        {
            ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();

            bool updateAvailable = latestRelease != null && latestRelease.TagName != TradeBot.Version;
            bool updateRequired = latestRelease?.Prerelease == false && IsUpdateRequired(latestRelease?.Body);
            string? newVersion = latestRelease?.TagName;

            if (updateAvailable || forceShow)
            {
                var updateForm = new UpdateForm(updateRequired, newVersion ?? "", updateAvailable);
                updateForm.ShowDialog();
            }

            return (updateAvailable, updateRequired, newVersion ?? string.Empty);
        }

        public static async Task<string> FetchChangelogAsync()
        {
            ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();
            return latestRelease?.Body ?? "Failed to fetch the latest release information.";
        }

        public static async Task<string?> FetchDownloadUrlAsync()
        {
            ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();
            if (latestRelease?.Assets == null)
                return null;

            return latestRelease.Assets
                .FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                ?.BrowserDownloadUrl;
        }

        private static async Task<ReleaseInfo?> FetchLatestReleaseAsync()
        {
            using var client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Add("User-Agent", "SysBot");

                string releasesUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
                HttpResponseMessage response = await client.GetAsync(releasesUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"GitHub API Error: {response.StatusCode} - {errorContent}");
                    return null;
                }

                string jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ReleaseInfo>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching release info: {ex.Message}");
                return null;
            }
        }

        private static bool IsUpdateRequired(string? changelogBody)
        {
            return !string.IsNullOrWhiteSpace(changelogBody) &&
                   changelogBody.Contains("Required = Yes", StringComparison.OrdinalIgnoreCase);
        }

        private class ReleaseInfo
        {
            [JsonProperty("tag_name")]
            public string? TagName { get; set; }

            [JsonProperty("prerelease")]
            public bool Prerelease { get; set; }

            [JsonProperty("assets")]
            public List<AssetInfo>? Assets { get; set; }

            [JsonProperty("body")]
            public string? Body { get; set; }
        }

        private class AssetInfo
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}
