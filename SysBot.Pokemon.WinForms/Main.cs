using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.WinForms
{
    public sealed partial class Main : Form
    {
        private static readonly string WorkingDirectory = Application.StartupPath;
        private static readonly string ConfigPath = Path.Combine(WorkingDirectory, "config.json");
        private readonly List<PokeBotConfig> Bots = new List<PokeBotConfig>();
        private readonly PokeBotRunner RunningEnvironment;

        public Main()
        {
            InitializeComponent();
            PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllText(ConfigPath);
                var prog = JsonConvert.DeserializeObject<ProgramConfig>(lines);
                RunningEnvironment = new PokeBotRunnerImpl(prog.Hub);
                foreach (var bot in prog.Bots)
                {
                    bot.Initialize();
                    AddBot(bot);
                }
            }
            else
            {
                var hub = new PokeTradeHubConfig();
                RunningEnvironment = new PokeBotRunnerImpl(hub);
                hub.Folder.CreateDefaults(WorkingDirectory);
            }

            LoadControls();
            Task.Run(BotMonitor);
        }

        private async Task BotMonitor()
        {
            while (!Disposing)
            {
                foreach (var c in FLP_Bots.Controls.OfType<BotController>())
                    c.ReadState();
                await Task.Delay(2_000).ConfigureAwait(false);
            }
        }

        private void LoadControls()
        {
            MinimumSize = Size;
            PG_Hub.SelectedObject = RunningEnvironment.Hub.Config;

            var routines = (PokeRoutineType[]) Enum.GetValues(typeof(PokeRoutineType));
            var list = routines.Select(z => new ComboItem(z.ToString(), (int) z)).ToArray();
            CB_Routine.DisplayMember = nameof(ComboItem.Text);
            CB_Routine.ValueMember = nameof(ComboItem.Value);
            CB_Routine.DataSource = list;
            CB_Routine.SelectedIndex = 2; // default option

            LogUtil.Forwarders.Add(AppendLog);
        }

        private void AppendLog(string message, string identity)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] - {identity}: {message}{Environment.NewLine}";
            if (InvokeRequired)
                Invoke((MethodInvoker)(() => UpdateLog(line)));
            else
                UpdateLog(line);
        }

        private void UpdateLog(string line)
        {
            // ghetto truncate
            if (RTB_Logs.Lines.Length > 99_999)
                RTB_Logs.Lines = RTB_Logs.Lines.Skip(25_0000).ToArray();

            RTB_Logs.AppendText(line);
            RTB_Logs.ScrollToCaret();
        }

        private ProgramConfig GetCurrentConfiguration()
        {
            return new ProgramConfig
            {
                Bots = Bots.ToArray(),
                Hub = RunningEnvironment.Hub.Config,
            };
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            var bots = RunningEnvironment;
            if (bots.IsRunning)
            {
                bots.StopAll();
                Thread.Sleep(100); // wait for things to abort?
            }

            SaveCurrentConfig();
        }

        private void SaveCurrentConfig()
        {
            var cfg = GetCurrentConfiguration();
            var lines = JsonConvert.SerializeObject(cfg);
            File.WriteAllText(ConfigPath, lines);
        }

        private void B_Start_Click(object sender, EventArgs e)
        {
            /*
            SaveCurrentConfig();

            LogUtil.LogInfo("Starting all bots...", "Form");
            RunningEnvironment.InitializeStart();
            SendAll(BotControlCommand.Start);
            Tab_Logs.Select();

            if (Bots.Count == 0)
                WinFormsUtil.Alert("No bots configured, but all supporting services have been started.");
            */
            Process.Start("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
            WinFormsUtil.Error("Extremely impatient user detected!!! Please consider replacing user!!!");
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

        private bool AddBot(PokeBotConfig cfg)
        {
            if (!cfg.IsValidIP())
                return false;

            var newbot = RunningEnvironment.CreateBotFromConfig(cfg);
            try
            {
                RunningEnvironment.Add(newbot);
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

        private void AddBotControl(PokeBotConfig cfg)
        {
            var row = new BotController {Width = FLP_Bots.Width};
            row.Initialize(RunningEnvironment, cfg);
            FLP_Bots.Controls.Add(row);
            FLP_Bots.SetFlowBreak(row, true);
            row.Click += (s, e) =>
            {
                TB_IP.Text = cfg.IP;
                NUD_Port.Value = cfg.Port;
                CB_Routine.SelectedValue = (int)cfg.InitialRoutine;
            };

            row.Remove += (s, e) =>
            {
                Bots.Remove(row.Config);
                RunningEnvironment.Remove(row.Config.IP, !RunningEnvironment.Hub.Config.SkipConsoleBotCreation);
                FLP_Bots.Controls.Remove(row);
            };
        }

        private PokeBotConfig CreateNewBotConfig()
        {
            var type = (PokeRoutineType)WinFormsUtil.GetIndex(CB_Routine);
            var ip = TB_IP.Text;
            var port = (int)NUD_Port.Value;

            var cfg = SwitchBotConfig.GetConfig<PokeBotConfig>(ip, port);
            cfg.Initialize(type);
            return cfg;
        }

        private void FLP_Bots_Resize(object sender, EventArgs e)
        {
            foreach (var c in FLP_Bots.Controls.OfType<BotController>())
                c.Width = FLP_Bots.Width;
        }
    }
}
