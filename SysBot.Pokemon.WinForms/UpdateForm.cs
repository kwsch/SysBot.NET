using System;
using System.Diagnostics;
using System.Drawing;
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

        public UpdateForm(bool updateRequired, string newVersion)
        {
            isUpdateRequired = updateRequired;
            this.newVersion = newVersion;
            InitializeComponent();
            Load += async (sender, e) => await FetchAndDisplayChangelog();
            if (isUpdateRequired)
            {
                labelUpdateInfo.Text = "A required update is available. You must update to continue using this application.";
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

        private async void ButtonDownload_Click(object sender, EventArgs e)
        {
            _ = new UpdateChecker();
            string downloadUrl = await UpdateChecker.FetchDownloadUrlAsync();
            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                StartDownloadProcess(downloadUrl);
            }
            else
            {
                MessageBox.Show("Failed to fetch the download URL. Please check your internet connection and try again.", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (isUpdateRequired)
            {
                Application.Exit();
            }
            else
            {
                MessageBox.Show("An update is available. Please close this program and replace it with the one that just downloaded.", "Update Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (isUpdateRequired && e.CloseReason == CloseReason.UserClosing)
            {
                // Prevent the form from closing
                e.Cancel = true;
                MessageBox.Show("This update is required. Please download and install the new version to continue using the application.", "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void StartDownloadProcess(string downloadUrl)
        {
            Main.IsUpdating = true;
            // Start the download by opening the URL in the default web browser
            Process.Start(new ProcessStartInfo
            {
                FileName = downloadUrl,
                UseShellExecute = true
            });
        }
    }
}
