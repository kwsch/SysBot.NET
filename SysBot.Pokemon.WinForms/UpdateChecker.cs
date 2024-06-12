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
        private const string RepositoryOwner = "bdawg1989";

        private const string RepositoryName = "MergeBot";

        public static async Task<(bool UpdateAvailable, bool UpdateRequired, string NewVersion)> CheckForUpdatesAsync()
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ReleaseInfo latestRelease = await FetchLatestReleaseAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            bool updateAvailable = latestRelease != null && latestRelease.TagName != TradeBot.Version;
#pragma warning disable CS8604 // Possible null reference argument.
            bool updateRequired = latestRelease?.Prerelease == false && IsUpdateRequired(latestRelease.Body);
#pragma warning restore CS8604 // Possible null reference argument.
            string? newVersion = latestRelease?.TagName;

            if (updateAvailable)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                UpdateForm updateForm = new(updateRequired, newVersion);
#pragma warning restore CS8604 // Possible null reference argument.
                updateForm.ShowDialog();
            }

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return (updateAvailable, updateRequired, newVersion);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

        public static async Task<string> FetchChangelogAsync()
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ReleaseInfo latestRelease = await FetchLatestReleaseAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (latestRelease == null)
                return "Failed to fetch the latest release information.";

#pragma warning disable CS8603 // Possible null reference return.
            return latestRelease.Body;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static async Task<string?> FetchDownloadUrlAsync()
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ReleaseInfo latestRelease = await FetchLatestReleaseAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (latestRelease == null)
                return null;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string? downloadUrl = latestRelease.Assets.Find(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))?.BrowserDownloadUrl;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.

            return downloadUrl;
        }

        private static async Task<ReleaseInfo?> FetchLatestReleaseAsync()
        {
            using var client = new HttpClient();
            try
            {
                // Add a custom header to identify the request
                client.DefaultRequestHeaders.Add("User-Agent", "MergeBot");

                string releasesUrl = $"http://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
                HttpResponseMessage response = await client.GetAsync(releasesUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonContent = await response.Content.ReadAsStringAsync();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                ReleaseInfo release = JsonConvert.DeserializeObject<ReleaseInfo>(jsonContent);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                return release;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsUpdateRequired(string changelogBody)
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
