using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Z3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysBot.Pokemon.Helpers;
using System.Drawing;
using SysBot.Pokemon.WinForms.Properties;

namespace SysBot.Pokemon.WinForms;

public sealed partial class Main : Form
{
    private readonly List<PokeBotState> Bots = [];


    private IPokeBotRunner RunningEnvironment { get; set; }
    private ProgramConfig Config { get; set; }
    public static bool IsUpdating { get; set; } = false;

    private bool _isFormLoading = true;
    public Main()
    {
        InitializeComponent();
        comboBox1.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);
        this.Load += async (sender, e) => await InitializeAsync();

    }

    private async Task InitializeAsync()
    {
        if (IsUpdating)
            return;
        PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();
        // Update checker
        UpdateChecker updateChecker = new UpdateChecker();
        await updateChecker.CheckForUpdatesAsync();
        if (File.Exists(Program.ConfigPath))
        {
            var lines = File.ReadAllText(Program.ConfigPath);
            Config = JsonSerializer.Deserialize(lines, ProgramConfigContext.Default.ProgramConfig) ?? new ProgramConfig();
            LogConfig.MaxArchiveFiles = Config.Hub.MaxArchiveFiles;
            LogConfig.LoggingEnabled = Config.Hub.LoggingEnabled;
            comboBox1.SelectedValue = (int)Config.Mode;
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
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "NotPaldea.net" : Config.Hub.BotName)} {TradeBot.Version} ({Config.Mode})";
        Task.Run(BotMonitor);
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

        comboBox1.DisplayMember = "Text";
        comboBox1.ValueMember = "Value";
        comboBox1.DataSource = gameModes;

        // Set the current mode as selected in the dropdown
        comboBox1.SelectedValue = (int)Config.Mode;

        comboBox2.Items.Add("Light Mode");
        comboBox2.Items.Add("Dark Mode");
        comboBox2.Items.Add("Poke Mode");
        comboBox2.Items.Add("Gengar Mode");
        comboBox2.Items.Add("Sylveon Mode");

        // Load the current theme from configuration and set it in the comboBox2
        string theme = Config.Hub.ThemeOption;
        if (string.IsNullOrEmpty(theme) || !comboBox2.Items.Contains(theme))
        {
            comboBox2.SelectedIndex = 0;  // Set default selection to Light Mode if ThemeOption is empty or invalid
        }
        else
        {
            comboBox2.SelectedItem = theme;  // Set the selected item in the combo box based on ThemeOption
        }
        switch (theme)
        {
            case "Dark Mode":
                ApplyDarkTheme();
                break;
            case "Light Mode":
                ApplyLightTheme();
                break;
            case "Poke Mode":
                ApplyPokemonTheme();
                break;
            case "Gengar Mode":
                ApplyGengarTheme();
                break;
            case "Sylveon Mode":
                ApplySylveonTheme();
                break;
            default:
                ApplyGengarTheme(); 
                break;
        }

        LogUtil.Forwarders.Add(new TextBoxForwarder(RTB_Logs));
    }

    private ProgramConfig GetCurrentConfiguration()
    {
        if (Config == null)
        {
            throw new InvalidOperationException("Config has not been initialized because a valid license was not entered.");
        }
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
    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isFormLoading) return; // Check to avoid processing during form loading

        if (comboBox1.SelectedValue is int selectedValue)
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
        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "NotPaldea.net" : Config.Hub.BotName)} {TradeBot.Version} ({Config.Mode})";
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

    private void UpdateBackgroundImage(ProgramMode mode)
    {
        switch (mode)
        {
            case ProgramMode.SV:
                FLP_Bots.BackgroundImage = Resources.sv_mode_image;
                break;
            case ProgramMode.SWSH:
                FLP_Bots.BackgroundImage = Resources.swsh_mode_image;
                break;
            case ProgramMode.BDSP:
                FLP_Bots.BackgroundImage = Resources.bdsp_mode_image;
                break;
            case ProgramMode.LA:
                FLP_Bots.BackgroundImage = Resources.pla_mode_image;
                break;
            case ProgramMode.LGPE:
                FLP_Bots.BackgroundImage = Resources.lgpe_mode_image;
                break;
            default:
                FLP_Bots.BackgroundImage = null;
                break;
        }
        FLP_Bots.BackgroundImageLayout = ImageLayout.Center;
    }

    private void SendAll(BotControlCommand cmd)
    {
        foreach (var c in FLP_Bots.Controls.OfType<BotController>())
            c.SendCommand(cmd, false);

        EchoUtil.Echo($"All bots have been issued a command to {cmd}.");
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

    private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            string selectedTheme = comboBox.SelectedItem.ToString();
            Config.Hub.ThemeOption = selectedTheme;  // Save the selected theme to the config
            SaveCurrentConfig();  // Save the config to file

            switch (selectedTheme)
            {
                case "Light Mode":
                    ApplyLightTheme();
                    break;
                case "Dark Mode":
                    ApplyDarkTheme();
                    break;
                case "Poke Mode":
                    ApplyPokemonTheme();
                    break;
                case "Gengar Mode":
                    ApplyGengarTheme();
                    break;
                case "Sylveon Mode":
                    ApplySylveonTheme();
                    break;
                default:
                    ApplyGengarTheme();
                    break;
            }
        }
    }

        private void ApplySylveonTheme()
        {
            // Define Sylveon-theme colors
            Color SoftPink = Color.FromArgb(255, 182, 193);   // A soft pink color inspired by Sylveon's body
            Color DeepPink = Color.FromArgb(255, 105, 180);   // A deeper pink for contrast and visual interest
            Color SkyBlue = Color.FromArgb(135, 206, 250);    // A soft blue color inspired by Sylveon's eyes and ribbons
            Color DeepBlue = Color.FromArgb(70, 130, 180);   // A deeper blue for contrast
            Color ElegantWhite = Color.FromArgb(255, 255, 255);// An elegant white for background and contrast

            // Set the background color of the form
            this.BackColor = ElegantWhite;

            // Set the foreground color of the form (text color)
            this.ForeColor = DeepBlue;

            // Set the background color of the tab control
            TC_Main.BackColor = SkyBlue;

            // Set the background color of each tab page
            foreach (TabPage page in TC_Main.TabPages)
            {
                page.BackColor = ElegantWhite;
            }

            // Set the background color of the property grid
            PG_Hub.BackColor = ElegantWhite;
            PG_Hub.LineColor = SkyBlue;
            PG_Hub.CategoryForeColor = DeepBlue;
            PG_Hub.CategorySplitterColor = SkyBlue;
            PG_Hub.HelpBackColor = SoftPink;
            PG_Hub.HelpForeColor = DeepBlue;
            PG_Hub.ViewBackColor = ElegantWhite;
            PG_Hub.ViewForeColor = DeepBlue;

            // Set the background color of the rich text box
            RTB_Logs.BackColor = SoftPink;
            RTB_Logs.ForeColor = DeepBlue;

            // Set colors for other controls
            TB_IP.BackColor = SkyBlue;
            TB_IP.ForeColor = DeepBlue;

            CB_Routine.BackColor = SkyBlue;
            CB_Routine.ForeColor = DeepBlue;

            NUD_Port.BackColor = SkyBlue;
            NUD_Port.ForeColor = DeepBlue;

            B_New.BackColor = DeepPink;
            B_New.ForeColor = ElegantWhite;

            FLP_Bots.BackColor = ElegantWhite;

            CB_Protocol.BackColor = SkyBlue;
            CB_Protocol.ForeColor = DeepBlue;

            comboBox1.BackColor = SkyBlue;
            comboBox1.ForeColor = DeepBlue;

            B_Stop.BackColor = DeepPink;
            B_Stop.ForeColor = ElegantWhite;

            B_Start.BackColor = DeepPink;
            B_Start.ForeColor = ElegantWhite;
        }

        private void ApplyGengarTheme()
        {
            // Define Gengar-theme colors
            Color GengarPurple = Color.FromArgb(88, 88, 120);  // A muted purple, the main color of Gengar
            Color DarkShadow = Color.FromArgb(40, 40, 60);     // A deeper shade for shadowing and contrast
            Color GhostlyGrey = Color.FromArgb(200, 200, 215); // A soft grey for text and borders
            Color HauntingBlue = Color.FromArgb(80, 80, 160);  // A haunting blue for accenting and highlights
            Color MidnightBlack = Color.FromArgb(25, 25, 35);  // A near-black for the darkest areas

            // Set the background color of the form
            this.BackColor = MidnightBlack;

            // Set the foreground color of the form (text color)
            this.ForeColor = GhostlyGrey;

            // Set the background color of the tab control
            TC_Main.BackColor = GengarPurple;

            // Set the background color of each tab page
            foreach (TabPage page in TC_Main.TabPages)
            {
                page.BackColor = DarkShadow;
            }

            // Set the background color of the property grid
            PG_Hub.BackColor = DarkShadow;
            PG_Hub.LineColor = HauntingBlue;
            PG_Hub.CategoryForeColor = GhostlyGrey;
            PG_Hub.CategorySplitterColor = HauntingBlue;
            PG_Hub.HelpBackColor = DarkShadow;
            PG_Hub.HelpForeColor = GhostlyGrey;
            PG_Hub.ViewBackColor = DarkShadow;
            PG_Hub.ViewForeColor = GhostlyGrey;

            // Set the background color of the rich text box
            RTB_Logs.BackColor = MidnightBlack;
            RTB_Logs.ForeColor = GhostlyGrey;

            // Set colors for other controls
            TB_IP.BackColor = GengarPurple;
            TB_IP.ForeColor = GhostlyGrey;

            CB_Routine.BackColor = GengarPurple;
            CB_Routine.ForeColor = GhostlyGrey;

            NUD_Port.BackColor = GengarPurple;
            NUD_Port.ForeColor = GhostlyGrey;

            B_New.BackColor = HauntingBlue;
            B_New.ForeColor = GhostlyGrey;

            FLP_Bots.BackColor = DarkShadow;

            CB_Protocol.BackColor = GengarPurple;
            CB_Protocol.ForeColor = GhostlyGrey;

            comboBox1.BackColor = GengarPurple;
            comboBox1.ForeColor = GhostlyGrey;

            B_Stop.BackColor = HauntingBlue;
            B_Stop.ForeColor = GhostlyGrey;

            B_Start.BackColor = HauntingBlue;
            B_Start.ForeColor = GhostlyGrey;
        }

        private void ApplyLightTheme()
        {
            // Define the color palette
            Color SoftBlue = Color.FromArgb(235, 245, 251);
            Color GentleGrey = Color.FromArgb(245, 245, 245);
            Color DarkBlue = Color.FromArgb(26, 13, 171);

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

            // Set colors for other controls
            TB_IP.BackColor = Color.White;
            TB_IP.ForeColor = DarkBlue;

            CB_Routine.BackColor = Color.White;
            CB_Routine.ForeColor = DarkBlue;

            NUD_Port.BackColor = Color.White;
            NUD_Port.ForeColor = DarkBlue;

            B_New.BackColor = SoftBlue;
            B_New.ForeColor = DarkBlue;

            FLP_Bots.BackColor = GentleGrey;

            CB_Protocol.BackColor = Color.White;
            CB_Protocol.ForeColor = DarkBlue;

            comboBox1.BackColor = Color.White;
            comboBox1.ForeColor = DarkBlue;

            B_Stop.BackColor = SoftBlue;
            B_Stop.ForeColor = DarkBlue;

            B_Start.BackColor = SoftBlue;
            B_Start.ForeColor = DarkBlue;
        }

        private void ApplyPokemonTheme()
        {
            // Define Poke-theme colors
            Color PokeRed = Color.FromArgb(206, 12, 30);      // A classic red tone reminiscent of the Pokeball
            Color DarkPokeRed = Color.FromArgb(164, 10, 24);  // A darker shade of the PokeRed for contrast and depth
            Color SleekGrey = Color.FromArgb(46, 49, 54);     // A sleek grey for background and contrast
            Color SoftWhite = Color.FromArgb(230, 230, 230);  // A soft white for text and borders
            Color MidnightBlack = Color.FromArgb(18, 19, 20); // A near-black for darker elements and depth

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
            RTB_Logs.BackColor = MidnightBlack;
            RTB_Logs.ForeColor = SoftWhite;

            // Set colors for other controls
            TB_IP.BackColor = DarkPokeRed;
            TB_IP.ForeColor = SoftWhite;

            CB_Routine.BackColor = DarkPokeRed;
            CB_Routine.ForeColor = SoftWhite;

            NUD_Port.BackColor = DarkPokeRed;
            NUD_Port.ForeColor = SoftWhite;

            B_New.BackColor = PokeRed;
            B_New.ForeColor = SoftWhite;

            FLP_Bots.BackColor = SleekGrey;

            CB_Protocol.BackColor = DarkPokeRed;
            CB_Protocol.ForeColor = SoftWhite;

            comboBox1.BackColor = DarkPokeRed;
            comboBox1.ForeColor = SoftWhite;

            B_Stop.BackColor = PokeRed;
            B_Stop.ForeColor = SoftWhite;

            B_Start.BackColor = PokeRed;
            B_Start.ForeColor = SoftWhite;
        }

        private void ApplyDarkTheme()
        {
            // Define the dark theme colors
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

            // Set colors for other controls
            TB_IP.BackColor = LightGrey;
            TB_IP.ForeColor = SoftWhite;

            CB_Routine.BackColor = LightGrey;
            CB_Routine.ForeColor = SoftWhite;

            NUD_Port.BackColor = LightGrey;
            NUD_Port.ForeColor = SoftWhite;

            B_New.BackColor = DarkRed;
            B_New.ForeColor = SoftWhite;

            FLP_Bots.BackColor = DarkGrey;

            CB_Protocol.BackColor = LightGrey;
            CB_Protocol.ForeColor = SoftWhite;

            comboBox1.BackColor = LightGrey;
            comboBox1.ForeColor = SoftWhite;

            B_Stop.BackColor = DarkRed;
            B_Stop.ForeColor = SoftWhite;

            B_Start.BackColor = DarkRed;
            B_Start.ForeColor = SoftWhite;
        }
    }

