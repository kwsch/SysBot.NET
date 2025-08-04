using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.WinForms.Properties;
using SysBot.Pokemon.Z3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace SysBot.Pokemon.WinForms;

/* 
*** Thank You and Credits ***
We would like to express our sincere gratitude to the following individuals and organizations for their invaluable contributions to this program:
Core Development Team:
- Havokx, Co-Creator of DudeBot.NET
- Link2026, Co-Creator of DudeBot.NET

Special Thanks To:
- kwsch, Creator of SysBot.NET
- Gengar, Creator of Mergebot
- Secludedly, Creator of ZE-Fusionbot

First and foremost, I appreciate the opportunity to have been part of this program.
Understanding new concepts was challenging, but I’m grateful for the knowledge I gained.
Contributors that truly made a difference with their dedication.
Kindness and support from certain staff members did not go unnoticed.

Your program’s structure helped me grow, even if the journey had its difficulties.
Overall, I value the connections I made and the lessons learned along the way.

Despite the challenges, I recognize the effort put into this program’s curriculum.
Every participant’s experience is unique, and I’m thankful for the good moments.
Valuable skills were acquired, thanks to those who genuinely cared about student success.
Reflecting on my time here, I appreciate the resilience it helped me build.
You’ve contributed to my growth, and for that, I sincerely thank you.

From the heart! 
*/

public sealed partial class Main : Form
{
    private readonly List<PokeBotState> Bots = [];

    private IPokeBotRunner RunningEnvironment { get; set; }

    private ProgramConfig Config { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static bool IsUpdating { get; set; } = false;

    private bool _isFormLoading = true;

#pragma warning disable CS8618

    public Main()
#pragma warning restore CS8618
    {
        InitializeComponent();
        CB_Mode.SelectedIndexChanged += new EventHandler(CB_Mode_SelectedIndexChanged);
        Load += async (sender, e) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (IsUpdating)
            return;
        PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();

        // Update checker
        UpdateChecker updateChecker = new UpdateChecker();
        await UpdateChecker.CheckForUpdatesAsync();

        if (File.Exists(Program.ConfigPath))
        {
            var lines = File.ReadAllText(Program.ConfigPath);
            Config = JsonSerializer.Deserialize(lines, ProgramConfigContext.Default.ProgramConfig) ?? new ProgramConfig();
            LogConfig.MaxArchiveFiles = Config.Hub.MaxArchiveFiles;
            LogConfig.LoggingEnabled = Config.Hub.LoggingEnabled;
            CB_Mode.SelectedValue = (int)Config.Mode;
            RunningEnvironment = GetRunner(Config);
            foreach (var bot in Config.Bots)
            {
                bot.Initialize();
                AddBot(bot);
            }
        }
        else
        {
            Config = new ProgramConfig();
            RunningEnvironment = GetRunner(Config);
            Config.Hub.Folder.CreateDefaults(Program.WorkingDirectory);
        }

        RTB_Logs.MaxLength = 32_767; // character length
        LoadControls();
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "DudeBot.NET" : Config.Hub.BotName)} {DudeBot.Version} ({Config.Mode})";
        _ = Task.Run(BotMonitor);
        InitUtil.InitializeStubs(Config.Mode);
        _isFormLoading = false;
        UpdateBackgroundImage(Config.Mode);
    }

    private static IPokeBotRunner GetRunner(ProgramConfig cfg) => cfg.Mode switch
    {
        ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(cfg.Hub, new BotFactory8SWSH()),
        ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(cfg.Hub, new BotFactory8BS()),
        ProgramMode.LA => new PokeBotRunnerImpl<PA8>(cfg.Hub, new BotFactory8LA()),
        ProgramMode.SV => new PokeBotRunnerImpl<PK9>(cfg.Hub, new BotFactory9SV()),
        ProgramMode.LGPE => new PokeBotRunnerImpl<PB7>(cfg.Hub, new BotFactory7LGPE()),
        _ => throw new IndexOutOfRangeException("Unsupported mode."),
    };

    private async Task BotMonitor()
    {
        while (!Disposing)
        {
            try
            {
                foreach (var c in FLP_Bots.Controls.OfType<BotController>())
                    c.ReadState();
            }
            catch
            {
                // Updating the collection by adding/removing bots will change the iterator
                // Can try a for-loop or ToArray, but those still don't prevent concurrent mutations of the array.
                // Just try, and if failed, ignore. Next loop will be fine. Locks on the collection are kinda overkill, since this task is not critical.
            }
            await Task.Delay(2_000).ConfigureAwait(false);
        }
    }

    private void LoadControls()
    {
        MinimumSize = Size;
        PG_Hub.SelectedObject = RunningEnvironment.Config;

        var routines = ((PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType))).Where(z => RunningEnvironment.SupportsRoutine(z));
        var list = routines.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
        CB_Routine.DisplayMember = nameof(ComboItem.Text);
        CB_Routine.ValueMember = nameof(ComboItem.Value);
        CB_Routine.DataSource = list;
        CB_Routine.SelectedValue = (int)PokeRoutineType.FlexTrade; // default option

        var protocols = (SwitchProtocol[])Enum.GetValues(typeof(SwitchProtocol));
        var listP = protocols.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
        CB_Protocol.DisplayMember = nameof(ComboItem.Text);
        CB_Protocol.ValueMember = nameof(ComboItem.Value);
        CB_Protocol.DataSource = listP;
        CB_Protocol.SelectedIndex = (int)SwitchProtocol.WiFi; // default option

        // Populate the game mode dropdown
        var gameModes = Enum.GetValues(typeof(ProgramMode))
            .Cast<ProgramMode>()
            .Where(m => m != ProgramMode.None) // Exclude the 'None' value
            .Select(mode => new { Text = mode.ToString(), Value = (int)mode })
        .ToList();

        CB_Mode.DisplayMember = "Text";
        CB_Mode.ValueMember = "Value";
        CB_Mode.DataSource = gameModes;

        // Set the current mode as selected in the dropdown
        CB_Mode.SelectedValue = (int)Config.Mode;

        CB_Theme.Items.Add("Light Theme");
        CB_Theme.Items.Add("Dark Theme");
        CB_Theme.Items.Add("Poké Ball Theme");
        CB_Theme.Items.Add("Lanturn Theme");
        CB_Theme.Items.Add("Dialga Theme");
        CB_Theme.Items.Add("Psyduck Theme");
        CB_Theme.Items.Add("Machamp Theme");
        CB_Theme.Items.Add("Pitch Black Theme");

        // Load the current theme from configuration and set it in the CB_Theme
        string theme = Config.Hub.ThemeOption;
        if (string.IsNullOrEmpty(theme) || !CB_Theme.Items.Contains(theme))
        {
            CB_Theme.SelectedIndex = 0;  // Set default selection to Light Mode if ThemeOption is empty or invalid
        }
        else
        {
            CB_Theme.SelectedItem = theme;  // Set the selected item in the combo box based on ThemeOption
        }
        switch (theme)
        {
            case "Light Theme":
                ApplyLightTheme();
                break;
            case "Dark Theme":
                ApplyDarkTheme();
                break;
            case "Poké Ball Theme":
                ApplyPokeballTheme();
                break;
            case "Lanturn Theme":
                ApplyLanturnTheme();
                break;
            case "Dialga Theme":
                ApplyDialgaTheme();
                break;
            case "Psyduck Theme":
                ApplyPsyduckTheme();
                break;
            case "Machamp Theme":
                ApplyMachampTheme();
                break;
            case "Pitch Black Theme":
                ApplyPitchBlackTheme();
                break;
            default:
                ApplyLightTheme();
                break;
        }

        LogUtil.Forwarders.Add(new TextBoxForwarder(RTB_Logs));
    }

    private ProgramConfig GetCurrentConfiguration()
    {

        Config.Bots = Bots.ToArray();
        return Config;
    }

    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (IsUpdating)
        {
            return;
        }
        SaveCurrentConfig();
        var bots = RunningEnvironment;
        if (!bots.IsRunning)
            return;

        async Task WaitUntilNotRunning()
        {
            while (bots.IsRunning)
                await Task.Delay(10).ConfigureAwait(false);
        }

        // Try to let all bots hard-stop before ending execution of the entire program.
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        bots.StopAll();
        Task.WhenAny(WaitUntilNotRunning(), Task.Delay(5_000)).ConfigureAwait(true).GetAwaiter().GetResult();
    }

    private void SaveCurrentConfig()
    {
        var cfg = GetCurrentConfiguration();
        var lines = JsonSerializer.Serialize(cfg, ProgramConfigContext.Default.ProgramConfig);
        File.WriteAllText(Program.ConfigPath, lines);
    }

    [JsonSerializable(typeof(ProgramConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public sealed partial class ProgramConfigContext : JsonSerializerContext;

    private void CB_Mode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isFormLoading) return; // Check to avoid processing during form loading

        if (CB_Mode.SelectedValue is int selectedValue)
        {
            ProgramMode newMode = (ProgramMode)selectedValue;
            Config.Mode = newMode;

            SaveCurrentConfig();
            UpdateRunnerAndUI();

            UpdateBackgroundImage(newMode);
        }
    }

    private void UpdateRunnerAndUI()
    {
        RunningEnvironment = GetRunner(Config);
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "DudeBot.NET" : Config.Hub.BotName)} {DudeBot.Version} ({Config.Mode})";
    }

    private void B_Start_Click(object sender, EventArgs e)
    {
        SaveCurrentConfig();

        LogUtil.LogInfo("Starting all bots...", "Form");
        RunningEnvironment.InitializeStart();
        SendAll(BotControlCommand.Start);
        Tab_Logs.Select();

        if (Bots.Count == 0)
            WinFormsUtil.Alert("No bots configured, but all supporting services have been started.");
    }

    private void B_Restart_Click(object sender, EventArgs e)
    {
        B_Stop_Click(sender, e);
        Task.Run(async () =>
        {
            await Task.Delay(3_500).ConfigureAwait(false);
            SaveCurrentConfig();
            LogUtil.LogInfo("Restarting all the consoles...", "Form");
            RunningEnvironment.InitializeStart();
            SendAll(BotControlCommand.RebootAndStop);
            await Task.Delay(5_000).ConfigureAwait(false); // Add a delay before restarting the bot
            SendAll(BotControlCommand.Start); // Start the bot after the delay
            Tab_Logs.Select();
            if (Bots.Count == 0)
                WinFormsUtil.Alert("No bots configured, but all supporting services have been issued the reboot command.");
        });
    }

    private void UpdateBackgroundImage(ProgramMode mode)
    {
        FLP_Bots.BackgroundImage = mode switch
        {
            ProgramMode.SV => Resources.sv_mode_image,
            ProgramMode.SWSH => Resources.swsh_mode_image,
            ProgramMode.BDSP => Resources.bdsp_mode_image,
            ProgramMode.LA => Resources.pla_mode_image,
            ProgramMode.LGPE => Resources.lgpe_mode_image,
            _ => null,
        };
        FLP_Bots.BackgroundImageLayout = ImageLayout.Center;
    }

    private void SendAll(BotControlCommand cmd)
    {
        foreach (var c in FLP_Bots.Controls.OfType<BotController>())
            c.SendCommand(cmd);
    }

    private void B_Stop_Click(object sender, EventArgs e)
    {
        var env = RunningEnvironment;
        if (!env.IsRunning && (ModifierKeys & Keys.Alt) == 0)
        {
            WinFormsUtil.Alert("Nothing is currently running.");
            return;
        }

        var cmd = BotControlCommand.Stop;

        if ((ModifierKeys & Keys.Control) != 0 || (ModifierKeys & Keys.Shift) != 0) // either, because remembering which can be hard
        {
            if (env.IsRunning)
            {
                WinFormsUtil.Alert("Commanding all bots to Idle.", "Press Stop (without a modifier key) to hard-stop and unlock control, or press Stop with the modifier key again to resume.");
                cmd = BotControlCommand.Idle;
            }
            else
            {
                WinFormsUtil.Alert("Commanding all bots to resume their original task.", "Press Stop (without a modifier key) to hard-stop and unlock control.");
                cmd = BotControlCommand.Resume;
            }
        }
        else
        {
            env.StopAll();
        }
        SendAll(cmd);
    }

    private void B_New_Click(object sender, EventArgs e)
    {
        var cfg = CreateNewBotConfig();
        if (!AddBot(cfg))
        {
            WinFormsUtil.Alert("Unable to add bot; ensure details are valid and not duplicate with an already existing bot.");
            return;
        }
        System.Media.SystemSounds.Asterisk.Play();
    }

    private async void B_Update_Click(object sender, EventArgs e)
    {
        var (updateAvailable, updateRequired, newVersion) = await UpdateChecker.CheckForUpdatesAsync();
        if (!updateAvailable)
        {
            var result = MessageBox.Show(
                "You are on the latest version. Would you like to re-download the current version?",
                "Update Check",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                UpdateForm updateForm = new UpdateForm(updateRequired, newVersion, updateAvailable: false);
                updateForm.ShowDialog();
            }
        }
        else
        {
            UpdateForm updateForm = new UpdateForm(updateRequired, newVersion, updateAvailable: true);
            updateForm.ShowDialog();
        }
    }

    private bool AddBot(PokeBotState cfg)
    {
        if (!cfg.IsValid())
            return false;

        if (Bots.Any(z => z.Connection.Equals(cfg.Connection)))
            return false;

        PokeRoutineExecutorBase newBot;
        try
        {
            Console.WriteLine($"Current Mode ({Config.Mode}) does not support this type of bot ({cfg.CurrentRoutineType}).");
            newBot = RunningEnvironment.CreateBotFromConfig(cfg);
        }
        catch
        {
            return false;
        }

        try
        {
            RunningEnvironment.Add(newBot);
        }
        catch (ArgumentException ex)
        {
            WinFormsUtil.Error(ex.Message);
            return false;
        }

        AddBotControl(cfg);
        Bots.Add(cfg);
        return true;
    }

    private void AddBotControl(PokeBotState cfg)
    {
        var row = new BotController { Width = FLP_Bots.Width };
        row.Initialize(RunningEnvironment, cfg);
        FLP_Bots.Controls.Add(row);
        FLP_Bots.SetFlowBreak(row, true);
        row.Click += (s, e) =>
        {
            var details = cfg.Connection;
            TB_IP.Text = details.IP;
            NUD_Port.Value = details.Port;
            CB_Protocol.SelectedIndex = (int)details.Protocol;
            CB_Routine.SelectedValue = (int)cfg.InitialRoutine;
        };

        row.Remove += (s, e) =>
        {
            Bots.Remove(row.State);
            RunningEnvironment.Remove(row.State, !RunningEnvironment.Config.SkipConsoleBotCreation);
            FLP_Bots.Controls.Remove(row);
        };
    }

    private PokeBotState CreateNewBotConfig()
    {
        var ip = TB_IP.Text;
        var port = (int)NUD_Port.Value;
        var cfg = BotConfigUtil.GetConfig<SwitchConnectionConfig>(ip, port);
        cfg.Protocol = (SwitchProtocol)WinFormsUtil.GetIndex(CB_Protocol);

        var pk = new PokeBotState { Connection = cfg };
        var type = (PokeRoutineType)WinFormsUtil.GetIndex(CB_Routine);
        pk.Initialize(type);
        return pk;
    }

    private void FLP_Bots_Resize(object sender, EventArgs e)
    {
        foreach (var c in FLP_Bots.Controls.OfType<BotController>())
            c.Width = FLP_Bots.Width;
    }

    private void CB_Protocol_SelectedIndexChanged(object sender, EventArgs e)
    {
        TB_IP.Visible = CB_Protocol.SelectedIndex == 0;
    }

    private void CB_Theme_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string selectedTheme = comboBox.SelectedItem.ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8601 // Possible null reference assignment.
            Config.Hub.ThemeOption = selectedTheme;  // Save the selected theme to the config
#pragma warning restore CS8601 // Possible null reference assignment.
            SaveCurrentConfig();  // Save the config to file

            switch (selectedTheme)
            {
                case "Light Theme":
                    ApplyLightTheme();
                    break;
                case "Dark Theme":
                    ApplyDarkTheme();
                    break;
                case "Poké Ball Theme":
                    ApplyPokeballTheme();
                    break;
                case "Lanturn Theme":
                    ApplyLanturnTheme();
                    break;
                case "Dialga Theme":
                    ApplyDialgaTheme();
                    break;
                case "Psyduck Theme":
                    ApplyPsyduckTheme();
                    break;
                case "Machamp Theme":
                    ApplyMachampTheme();
                    break;
                case "Pitch Black Theme":
                    ApplyPitchBlackTheme();
                    break;
                default:
                    ApplyLightTheme();
                    break;
            }
        }
    }

    private void ApplyLightTheme()
    {
        // Define the color palette
        Color SoftBlue = Color.FromArgb(235, 245, 251);
        Color GentleGrey = Color.FromArgb(245, 245, 245);
        Color DarkBlue = Color.FromArgb(26, 13, 171);
        Color HarderSoftBlue = Color.FromArgb(240, 245, 255);
        Color GentleGreen = Color.FromArgb(192, 255, 192);

        // Set the background color of the form
        this.BackColor = GentleGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = DarkBlue;

        // Set the background color of the tab control
        TC_Main.BackColor = SoftBlue;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = GentleGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = GentleGrey;
        PG_Hub.LineColor = SoftBlue;
        PG_Hub.CategoryForeColor = DarkBlue;
        PG_Hub.CategorySplitterColor = SoftBlue;
        PG_Hub.HelpBackColor = GentleGrey;
        PG_Hub.HelpForeColor = DarkBlue;
        PG_Hub.ViewBackColor = GentleGrey;
        PG_Hub.ViewForeColor = DarkBlue;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = Color.White;
        RTB_Logs.ForeColor = DarkBlue;

        //set color for layout panel
        FLP_Bots.BackColor = GentleGrey;

        // Set colors for Textboxes
        TB_IP.BackColor = Color.White;
        TB_IP.ForeColor = DarkBlue;

        NUD_Port.BackColor = Color.White;
        NUD_Port.ForeColor = DarkBlue;

        //Set colors for combo boxes
        CB_Routine.BackColor = Color.White;
        CB_Routine.ForeColor = DarkBlue;

        CB_Protocol.BackColor = Color.White;
        CB_Protocol.ForeColor = DarkBlue;

        CB_Mode.BackColor = Color.White;
        CB_Mode.ForeColor = DarkBlue;

        CB_Theme.BackColor = Color.White;
        CB_Theme.ForeColor = DarkBlue;

        // Set colors for buttons
        B_New.BackColor = SoftBlue;
        B_New.ForeColor = DarkBlue;

        B_Stop.BackColor = Color.Maroon;
        B_Stop.ForeColor = Color.WhiteSmoke;

        B_Start.BackColor = GentleGreen;
        B_Start.ForeColor = Color.ForestGreen;

        B_Restart.BackColor = Color.PowderBlue;
        B_Restart.ForeColor = Color.SteelBlue;

        B_Update.BackColor = Color.Gray;
        B_Update.ForeColor = Color.Gainsboro;

    }

    private void ApplyDarkTheme()
    {
        // Define the color palette
        Color DarkRed = Color.FromArgb(90, 0, 0);
        Color DarkGrey = Color.FromArgb(30, 30, 30);
        Color LightGrey = Color.FromArgb(60, 60, 60);
        Color SoftWhite = Color.FromArgb(245, 245, 245);

        // Set the background color of the form
        this.BackColor = DarkGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = LightGrey;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DarkGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DarkGrey;
        PG_Hub.LineColor = LightGrey;
        PG_Hub.CategoryForeColor = SoftWhite;
        PG_Hub.CategorySplitterColor = LightGrey;
        PG_Hub.HelpBackColor = DarkGrey;
        PG_Hub.HelpForeColor = SoftWhite;
        PG_Hub.ViewBackColor = DarkGrey;
        PG_Hub.ViewForeColor = SoftWhite;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DarkGrey;
        RTB_Logs.ForeColor = SoftWhite;

        //set color for layout panel
        FLP_Bots.BackColor = DarkGrey;

        // Set colors for Textboxes
        TB_IP.BackColor = DarkRed;
        TB_IP.ForeColor = SoftWhite;

        NUD_Port.BackColor = DarkRed;
        NUD_Port.ForeColor = SoftWhite;

        //Set colors for combo boxes
        CB_Routine.BackColor = LightGrey;
        CB_Routine.ForeColor = SoftWhite;

        CB_Protocol.BackColor = LightGrey;
        CB_Protocol.ForeColor = SoftWhite;

        CB_Mode.BackColor = LightGrey;
        CB_Mode.ForeColor = SoftWhite;

        CB_Theme.BackColor = LightGrey;
        CB_Theme.ForeColor = SoftWhite;

        // Set colors for buttons
        B_New.BackColor = DarkRed;
        B_New.ForeColor = SoftWhite;

        B_Stop.BackColor = DarkRed;
        B_Stop.ForeColor = SoftWhite;

        B_Start.BackColor = DarkRed;
        B_Start.ForeColor = SoftWhite;

        B_Restart.BackColor = DarkRed;
        B_Restart.ForeColor = SoftWhite;

        B_Update.BackColor = DarkRed;
        B_Update.ForeColor = SoftWhite;
    }
    private void ApplyPokeballTheme()
    {
        // Define the color palette
        Color PokeRed = Color.FromArgb(206, 12, 30);
        Color DarkPokeRed = Color.FromArgb(164, 10, 24);
        Color SleekGrey = Color.FromArgb(46, 49, 54);
        Color SoftWhite = Color.FromArgb(230, 230, 230);
        Color MidnightBlack = Color.FromArgb(18, 19, 20);

        // Set the background color of the form
        this.BackColor = SleekGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = SoftWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = DarkPokeRed;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = SleekGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = SleekGrey;
        PG_Hub.LineColor = DarkPokeRed;
        PG_Hub.CategoryForeColor = SoftWhite;
        PG_Hub.CategorySplitterColor = DarkPokeRed;
        PG_Hub.HelpBackColor = SleekGrey;
        PG_Hub.HelpForeColor = SoftWhite;
        PG_Hub.ViewBackColor = SleekGrey;
        PG_Hub.ViewForeColor = SoftWhite;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = SleekGrey;
        RTB_Logs.ForeColor = SoftWhite;

        //set color for layout panel
        FLP_Bots.BackColor = SleekGrey;

        // Set colors for Textboxes
        TB_IP.BackColor = DarkPokeRed;
        TB_IP.ForeColor = SoftWhite;

        NUD_Port.BackColor = DarkPokeRed;
        NUD_Port.ForeColor = SoftWhite;

        //Set colors for combo boxes
        CB_Routine.BackColor = DarkPokeRed;
        CB_Routine.ForeColor = SoftWhite;

        CB_Protocol.BackColor = DarkPokeRed;
        CB_Protocol.ForeColor = SoftWhite;

        CB_Mode.BackColor = DarkPokeRed;
        CB_Mode.ForeColor = SoftWhite;

        CB_Theme.BackColor = DarkPokeRed;
        CB_Theme.ForeColor = SoftWhite;

        // Set colors for buttons
        B_New.BackColor = PokeRed;
        B_New.ForeColor = SoftWhite;

        B_Stop.BackColor = PokeRed;
        B_Stop.ForeColor = SoftWhite;

        B_Start.BackColor = PokeRed;
        B_Start.ForeColor = SoftWhite;

        B_Restart.BackColor = PokeRed;
        B_Restart.ForeColor = SoftWhite;

        B_Update.BackColor = PokeRed;
        B_Update.ForeColor = SoftWhite;
    }
    private void ApplyLanturnTheme()
    {
        // Define the color palette
        Color LanturnPurple = Color.FromArgb(112, 83, 162);
        Color LanturnYellow = Color.FromArgb(242, 253, 97);
        Color LanturnBlue = Color.FromArgb(121, 144, 197);
        Color LanturnWhite = Color.WhiteSmoke;


        // Set the background color of the form
        this.BackColor = LanturnPurple;

        // Set the foreground color of the form (text color)
        this.ForeColor = LanturnWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = LanturnPurple;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = LanturnPurple;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = LanturnPurple;
        PG_Hub.LineColor = LanturnBlue;
        PG_Hub.CategoryForeColor = LanturnWhite;
        PG_Hub.CategorySplitterColor = LanturnYellow;
        PG_Hub.HelpBackColor = LanturnPurple;
        PG_Hub.HelpForeColor = LanturnYellow;
        PG_Hub.ViewBackColor = LanturnPurple;
        PG_Hub.ViewForeColor = LanturnYellow;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = LanturnPurple;
        RTB_Logs.ForeColor = LanturnWhite;

        //set color for layout panel
        FLP_Bots.BackColor = LanturnPurple;

        // Set colors for Textboxes
        TB_IP.BackColor = LanturnBlue;
        TB_IP.ForeColor = LanturnYellow;

        NUD_Port.BackColor = LanturnBlue;
        NUD_Port.ForeColor = LanturnYellow;

        //Set colors for combo boxes
        CB_Routine.BackColor = LanturnPurple;
        CB_Routine.ForeColor = LanturnYellow;

        CB_Protocol.BackColor = LanturnPurple;
        CB_Protocol.ForeColor = LanturnYellow;

        CB_Mode.BackColor = LanturnPurple;
        CB_Mode.ForeColor = LanturnYellow;

        CB_Theme.BackColor = LanturnPurple;
        CB_Theme.ForeColor = LanturnYellow;

        // Set colors for buttons
        B_New.BackColor = LanturnBlue;
        B_New.ForeColor = LanturnYellow;

        B_Stop.BackColor = LanturnBlue;
        B_Stop.ForeColor = LanturnYellow;

        B_Start.BackColor = LanturnBlue;
        B_Start.ForeColor = LanturnYellow;

        B_Restart.BackColor = LanturnBlue;
        B_Restart.ForeColor = LanturnYellow;

        B_Update.BackColor = LanturnBlue;
        B_Update.ForeColor = LanturnYellow;
    }
    private void ApplyDialgaTheme()
    {
        // Define the color palette
        Color DialgaBlue = Color.FromArgb(32, 90, 148);
        Color DialgaGrey = Color.FromArgb(189, 205, 222);
        Color DialgaDGrey = Color.FromArgb(65, 65, 82);
        Color DialgaTeal = Color.FromArgb(98, 164, 197);

        // Set the background color of the form
        this.BackColor = DialgaDGrey;

        // Set the foreground color of the form (text color)
        this.ForeColor = DialgaGrey;

        // Set the background color of the tab control
        TC_Main.BackColor = DialgaDGrey;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = DialgaDGrey;
        }

        // Set the background color of the property grid
        PG_Hub.BackColor = DialgaDGrey;
        PG_Hub.LineColor = DialgaGrey;
        PG_Hub.CategoryForeColor = DialgaDGrey;
        PG_Hub.CategorySplitterColor = DialgaGrey;
        PG_Hub.HelpBackColor = DialgaDGrey;
        PG_Hub.HelpForeColor = DialgaGrey;
        PG_Hub.ViewBackColor = DialgaDGrey;
        PG_Hub.ViewForeColor = DialgaGrey;

        // Set the background color of the rich text box
        RTB_Logs.BackColor = DialgaDGrey;
        RTB_Logs.ForeColor = DialgaGrey;

        //set color for layout panel
        FLP_Bots.BackColor = DialgaDGrey;

        // Set colors for Textboxes
        TB_IP.BackColor = DialgaBlue;
        TB_IP.ForeColor = DialgaGrey;

        NUD_Port.BackColor = DialgaBlue;
        NUD_Port.ForeColor = DialgaGrey;

        //Set colors for combo boxes
        CB_Routine.BackColor = DialgaBlue;
        CB_Routine.ForeColor = DialgaGrey;

        CB_Protocol.BackColor = DialgaBlue;
        CB_Protocol.ForeColor = DialgaGrey;

        CB_Mode.BackColor = DialgaBlue;
        CB_Mode.ForeColor = DialgaGrey;

        CB_Theme.BackColor = DialgaBlue;
        CB_Theme.ForeColor = DialgaGrey;

        // Set colors for buttons
        B_New.BackColor = DialgaBlue;
        B_New.ForeColor = DialgaTeal;

        B_Stop.BackColor = DialgaBlue;
        B_Stop.ForeColor = DialgaTeal;

        B_Start.BackColor = DialgaBlue;
        B_Start.ForeColor = DialgaTeal;

        B_Restart.BackColor = DialgaBlue;
        B_Restart.ForeColor = DialgaTeal;

        B_Update.BackColor = DialgaBlue;
        B_Update.ForeColor = DialgaTeal;
    }
    private void ApplyPsyduckTheme()
    {
        // Define the colour palette
        Color PsyBlue = Color.FromArgb(101, 155, 175);
        Color PsyWhite = Color.FromArgb(230, 254, 253);

        // Set the background color of the Hub form
        this.BackColor = PsyBlue;

        // Set the foreground color of the main status form
        this.ForeColor = PsyWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = PsyBlue;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = PsyBlue;
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = PsyBlue;
        PG_Hub.LineColor = PsyWhite;
        PG_Hub.CategoryForeColor = PsyBlue;
        PG_Hub.CategorySplitterColor = PsyWhite;
        PG_Hub.HelpBackColor = PsyBlue;
        PG_Hub.HelpForeColor = PsyWhite;
        PG_Hub.ViewBackColor = PsyBlue;
        PG_Hub.ViewForeColor = PsyWhite;

        // Set the colors of the rich textboxes
        RTB_Logs.BackColor = PsyBlue;
        RTB_Logs.ForeColor = PsyWhite;

        //set color for layout panel
        FLP_Bots.BackColor = PsyBlue;

        // Set colors for Textboxes
        TB_IP.BackColor = PsyBlue;
        TB_IP.ForeColor = PsyWhite;

        NUD_Port.BackColor = PsyBlue;
        NUD_Port.ForeColor = PsyWhite;

        //Set colors for combo boxes
        CB_Routine.BackColor = PsyBlue;
        CB_Routine.ForeColor = PsyWhite;

        CB_Protocol.BackColor = PsyBlue;
        CB_Protocol.ForeColor = PsyWhite;

        CB_Mode.BackColor = PsyBlue;
        CB_Mode.ForeColor = PsyWhite;

        CB_Theme.BackColor = PsyBlue;
        CB_Theme.ForeColor = PsyWhite;

        // Set colors for buttons
        B_New.BackColor = PsyBlue;
        B_New.ForeColor = PsyWhite;

        B_Stop.BackColor = PsyBlue;
        B_Stop.ForeColor = PsyWhite;

        B_Start.BackColor = PsyBlue;
        B_Start.ForeColor = PsyWhite;

        B_Restart.BackColor = PsyBlue;
        B_Restart.ForeColor = PsyWhite;

        B_Update.BackColor = PsyBlue;
        B_Update.ForeColor = PsyWhite;
    }
    private void ApplyMachampTheme()
    {
        // Define the colour palette
        Color MachampGreen = Color.FromArgb(151, 188, 88);
        Color MachampDarkGreen = Color.FromArgb(37, 47, 10);
        Color MachampWhite = Color.FromArgb(251, 241, 189);
        Color MachampGrey = Color.FromArgb(185, 175, 159);

        // Set the background color of the Hub form
        this.BackColor = MachampGreen;

        // Set the foreground color of the main status form
        this.ForeColor = MachampWhite;

        // Set the background color of the tab control
        TC_Main.BackColor = MachampGreen;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = MachampGreen;
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = MachampDarkGreen;
        PG_Hub.LineColor = MachampGrey;
        PG_Hub.CategoryForeColor = MachampDarkGreen;
        PG_Hub.CategorySplitterColor = MachampGrey;
        PG_Hub.HelpBackColor = MachampDarkGreen;
        PG_Hub.HelpForeColor = MachampWhite;
        PG_Hub.ViewBackColor = MachampDarkGreen;
        PG_Hub.ViewForeColor = MachampWhite;

        // Set the colors of the rich textboxes
        RTB_Logs.BackColor = MachampDarkGreen;
        RTB_Logs.ForeColor = MachampWhite;

        //set color for layout panel
        FLP_Bots.BackColor = MachampDarkGreen;

        // Set colors for Textboxes
        TB_IP.BackColor = MachampDarkGreen;
        TB_IP.ForeColor = MachampWhite;

        NUD_Port.BackColor = MachampDarkGreen;
        NUD_Port.ForeColor = MachampWhite;

        //Set colors for combo boxes
        CB_Routine.BackColor = MachampDarkGreen;
        CB_Routine.ForeColor = MachampWhite;

        CB_Protocol.BackColor = MachampDarkGreen;
        CB_Protocol.ForeColor = MachampWhite;

        CB_Mode.BackColor = MachampDarkGreen;
        CB_Mode.ForeColor = MachampWhite;

        CB_Theme.BackColor = MachampDarkGreen;
        CB_Theme.ForeColor = MachampWhite;

        // Set colors for buttons
        B_New.BackColor = MachampGreen;
        B_New.ForeColor = MachampDarkGreen;

        B_Stop.BackColor = MachampGreen;
        B_Stop.ForeColor = MachampDarkGreen;

        B_Start.BackColor = MachampGreen;
        B_Start.ForeColor = MachampDarkGreen;

        B_Restart.BackColor = MachampGreen;
        B_Restart.ForeColor = MachampDarkGreen;

        B_Update.BackColor = MachampGreen;
        B_Update.ForeColor = MachampDarkGreen;
    }
    private void ApplyPitchBlackTheme()
    {
        // Define the colour palette
        Color White = Color.White;
        Color Black = Color.Black;
        Color Grey = Color.FromArgb(51, 51, 51);

        // Set the background color of the Hub form
        this.BackColor = Black;

        // Set the foreground color of the main status form
        this.ForeColor = White;

        // Set the background color of the tab control
        TC_Main.BackColor = Black;

        // Set the background color of each tab page
        foreach (TabPage page in TC_Main.TabPages)
        {
            page.BackColor = Black;
        }

        // Set the background color of the Hub
        PG_Hub.BackColor = Black;
        PG_Hub.LineColor = Grey;
        PG_Hub.CategoryForeColor = White;
        PG_Hub.CategorySplitterColor = Grey;
        PG_Hub.HelpBackColor = Black;
        PG_Hub.HelpForeColor = White;
        PG_Hub.ViewBackColor = Black;
        PG_Hub.ViewForeColor = White;

        // Set the colors of the rich textboxes
        RTB_Logs.BackColor = Black;
        RTB_Logs.ForeColor = White;

        //set color for layout panel
        FLP_Bots.BackColor = Black;

        // Set colors for Textboxes
        TB_IP.BackColor = Black;
        TB_IP.ForeColor = White;

        NUD_Port.BackColor = Black;
        NUD_Port.ForeColor = White;

        //Set colors for combo boxes
        CB_Routine.BackColor = Black;
        CB_Routine.ForeColor = White;

        CB_Protocol.BackColor = Black;
        CB_Protocol.ForeColor = White;

        CB_Mode.BackColor = Black;
        CB_Mode.ForeColor = White;

        CB_Theme.BackColor = Black;
        CB_Theme.ForeColor = White;

        // Set colors for buttons
        B_New.BackColor = Black;
        B_New.ForeColor = White;

        B_Stop.BackColor = Black;
        B_Stop.ForeColor = White;

        B_Start.BackColor = Black;
        B_Start.ForeColor = White;

        B_Restart.BackColor = Black;
        B_Restart.ForeColor = White;

        B_Update.BackColor = Black;
        B_Update.ForeColor = White;
    }
}
