using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysBot.Pokemon.Helpers;

public class UpdateChecker
{
    private const string VersionUrl = "https://genpkm.com/tradebot/version.txt";

    // This method is now updated to return a tuple containing version and required flag
    public async Task<(bool UpdateAvailable, bool UpdateRequired)> CheckForUpdatesAsync()
    {
        var versionInfo = await FetchVersionInfoAsync();
        bool updateAvailable = !string.IsNullOrEmpty(versionInfo.Version) && versionInfo.Version != TradeBot.Version;
        bool updateRequired = versionInfo.UpdateRequired;

        if (updateAvailable)
        {
            UpdateForm updateForm = new UpdateForm(updateRequired); // Pass the required update flag to the form
            updateForm.ShowDialog();
        }

        return (updateAvailable, updateRequired);
    }

    // Fetch and parse the version information
    private async Task<(string Version, bool UpdateRequired)> FetchVersionInfoAsync()
    {
        using (var client = new HttpClient())
        {
            try
            {
                string content = await client.GetStringAsync(VersionUrl);
                var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var versionLine = lines[0].Trim();
                var requiredLine = lines.Length > 1 ? lines[1].Trim() : "";
                var required = requiredLine.Equals("required=yes", StringComparison.OrdinalIgnoreCase);

                return (versionLine, required);
            }
            catch (Exception)
            {
                // Handle exceptions (e.g., network errors)
                return (null, false);
            }
        }
    }

    // Add a method to fetch the changelog
    public async Task<string> FetchChangelogAsync()
    {
        using (var client = new HttpClient())
        {
            try
            {
                string changelog = await client.GetStringAsync("https://genpkm.com/tradebot/changelog.txt");
                return changelog.Trim();
            }
            catch (Exception)
            {
                // Handle exceptions (e.g., network errors)
                return "Changelog is not available at the moment.";
            }
        }
    }
}

