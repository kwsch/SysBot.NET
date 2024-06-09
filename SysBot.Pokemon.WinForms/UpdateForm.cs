using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    public class UpdateForm : Form
    {
        private Button buttonDownload;
        private Label labelUpdateInfo;
        private readonly Label labelChangelogTitle = new();
        private TextBox textBoxChangelog;
        private readonly bool isUpdateRequired;
        private readonly string newVersion;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public UpdateForm(bool updateRequired, string newVersion)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            isUpdateRequired = updateRequired;
            this.newVersion = newVersion;
            InitializeComponent();
            Load += async (sender, e) => await FetchAndDisplayChangelog();
            if (isUpdateRequired)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                labelUpdateInfo.Text = "A required update is available. You must update to continue using this application.";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                // Optionally, you can also disable the close button on the form if the update is required
                ControlBox = false;
            }
        }

        private void InitializeComponent()
        {
            labelUpdateInfo = new Label();
            buttonDownload = new Button();

            // Update the size of the form
            ClientSize = new Size(500, 300); // New width and height

            // labelUpdateInfo
            labelUpdateInfo.AutoSize = true;
            labelUpdateInfo.Location = new Point(12, 20); // Adjust as needed
            labelUpdateInfo.Size = new Size(460, 60); // Adjust as needed
            labelUpdateInfo.Text = $"A new version is available. Please download the latest version.";

            // buttonDownload
            buttonDownload.Size = new Size(130, 23); // Set the button size if not already set
            int buttonX = (ClientSize.Width - buttonDownload.Size.Width) / 2; // Calculate X position
            int buttonY = ClientSize.Height - buttonDownload.Size.Height - 20; // Calculate Y position, 20 pixels from the bottom
            buttonDownload.Location = new Point(buttonX, buttonY);
            buttonDownload.Text = $"Download Update";
            buttonDownload.Click += ButtonDownload_Click;

            // labelChangelogTitle
            labelChangelogTitle.AutoSize = true;
            labelChangelogTitle.Location = new Point(10, 60); // Set this Y position above textBoxChangelog
            labelChangelogTitle.Size = new Size(70, 15); // Set an appropriate size or leave it to AutoSize
            labelChangelogTitle.Font = new Font(labelChangelogTitle.Font.FontFamily, 11, FontStyle.Bold);
            labelChangelogTitle.Text = $"Changelog ({newVersion}):";

            // textBoxChangelog
            // Adjust the size and position to fit the new form dimensions
            textBoxChangelog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 90), // Adjust as needed
                Size = new Size(480, 150), // Adjust as needed to fit the new form size
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };

            // UpdateForm
            Controls.Add(labelUpdateInfo);
            Controls.Add(buttonDownload);
            Controls.Add(labelChangelogTitle);
            Controls.Add(textBoxChangelog);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = $"Update Available ({newVersion})";
        }

        private async Task FetchAndDisplayChangelog()
        {
            _ = new UpdateChecker();
            string changelog = await UpdateChecker.FetchChangelogAsync();
            textBoxChangelog.Text = changelog;
        }

        private async void ButtonDownload_Click(object? sender, EventArgs? e)
        {
            string? downloadUrl = await UpdateChecker.FetchDownloadUrlAsync();
            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                string downloadedFilePath = await StartDownloadProcessAsync(downloadUrl);
                if (!string.IsNullOrEmpty(downloadedFilePath))
                {
                    // Close the application
                    Application.Exit();

                    // Start a new process to replace the executable and restart the application
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C timeout /t 1 & move /y \"{downloadedFilePath}\" \"{Application.ExecutablePath}\" & start \"\" \"{Application.ExecutablePath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            else
            {
                MessageBox.Show("Failed to fetch the download URL. Please check your internet connection and try again.", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task<string> StartDownloadProcessAsync(string downloadUrl)
        {
            Main.IsUpdating = true;
            string downloadedFilePath = Path.Combine(Application.StartupPath, "SysBot.Pokemon.WinForms.exe");
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(downloadedFilePath, fileBytes);
            }
            return downloadedFilePath;
        }
    }
}
