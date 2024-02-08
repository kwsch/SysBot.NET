using System;
using System.Windows.Forms;
using System.Diagnostics;
using SysBot.Pokemon.WinForms;
using System.Drawing;
using System.Threading.Tasks;

public class UpdateForm : Form
{
    private Button buttonDownload;
    private Label labelUpdateInfo;
    Label labelChangelogTitle = new Label();
    private TextBox textBoxChangelog;
    private bool isUpdateRequired;

    public UpdateForm(bool updateRequired)
    {
        isUpdateRequired = updateRequired;
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
        this.ClientSize = new System.Drawing.Size(500, 300); // New width and height

        // labelUpdateInfo
        labelUpdateInfo.AutoSize = true;
        labelUpdateInfo.Location = new System.Drawing.Point(12, 20); // Adjust as needed
        labelUpdateInfo.Size = new System.Drawing.Size(460, 60); // Adjust as needed
        labelUpdateInfo.Text = "A new version is available. Please download the latest version.";

        // buttonDownload
        buttonDownload.Size = new System.Drawing.Size(130, 23); // Set the button size if not already set
        int buttonX = (this.ClientSize.Width - buttonDownload.Size.Width) / 2; // Calculate X position
        int buttonY = this.ClientSize.Height - buttonDownload.Size.Height - 20; // Calculate Y position, 20 pixels from the bottom
        buttonDownload.Location = new System.Drawing.Point(buttonX, buttonY);
        buttonDownload.Text = "Download Update";
        buttonDownload.Click += ButtonDownload_Click;

        // labelChangelogTitle
        labelChangelogTitle.AutoSize = true;
        labelChangelogTitle.Location = new System.Drawing.Point(10, 60); // Set this Y position above textBoxChangelog
        labelChangelogTitle.Size = new System.Drawing.Size(70, 15); // Set an appropriate size or leave it to AutoSize
        labelChangelogTitle.Font = new Font(labelChangelogTitle.Font.FontFamily, 11, FontStyle.Bold);
        labelChangelogTitle.Text = "Changelog:";

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
        this.Controls.Add(this.labelUpdateInfo);
        this.Controls.Add(this.buttonDownload);
        this.Controls.Add(labelChangelogTitle);
        this.Controls.Add(this.textBoxChangelog);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "UpdateForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Update Available";
    }

    private async Task FetchAndDisplayChangelog()
    {
        UpdateChecker updateChecker = new UpdateChecker();
        string changelog = await updateChecker.FetchChangelogAsync();
        textBoxChangelog.Text = changelog;
    }
    private void ButtonDownload_Click(object sender, EventArgs e)
    {
        StartDownloadProcess();
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

    private void StartDownloadProcess()
    {
        Main.IsUpdating = true;
        // Start the download by opening the URL in the default web browser
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://genpkm.com/tradebot/SysBot.exe",
            UseShellExecute = true
        });
    }
}

