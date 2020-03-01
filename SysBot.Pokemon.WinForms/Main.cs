using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly PokeTradeHub<PK8> Hub;

        private BotEnvironment? RunningEnvironment;

        public Main()
        {
            InitializeComponent();
            MinimumSize = Size;
            LV_Bots.Items.Clear();

            Hub = GetInitialHub();
            PG_Hub.SelectedObject = Hub.Config;

            var routines = (PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType));
            var list = routines.Select(z => new ComboItem(z.ToString(), (int)z)).ToArray();
            CB_Routine.DisplayMember = nameof(ComboItem.Text);
            CB_Routine.ValueMember = nameof(ComboItem.Value);
            CB_Routine.DataSource = list;
            CB_Routine.SelectedIndex = 2; // default option

            LogUtil.Forwarders.Add(AppendLog);
        }

        private PokeTradeHub<PK8> GetInitialHub()
        {
            PokeTradeHubConfig cfg;

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllText(ConfigPath);
                var prog = JsonConvert.DeserializeObject<ProgramConfig>(lines);
                foreach (var bot in prog.Bots)
                {
                    bot.Initialize();
                    AddBot(bot);
                }
                cfg = prog.Hub;
            }
            else
            {
                cfg = new PokeTradeHubConfig();
                cfg.Folder.CreateDefaults(WorkingDirectory);
            }

            return new PokeTradeHub<PK8>(cfg);
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

            var bot = RunningEnvironment?.Bots.Find(z => line.Contains(z.Connection.Name));
            if (bot == null)
                return;

            var index = RunningEnvironment!.Bots.IndexOf(bot);
            var start = line.IndexOf(bot.Connection.Name, StringComparison.Ordinal);
            var substring = line.Substring(start).Trim();
            LV_Bots.Items[index].SubItems[3].Text = substring;
        }

        private ProgramConfig GetCurrentConfiguration()
        {
            return new ProgramConfig
            {
                Bots = Bots.ToArray(),
                Hub = Hub.Config,
            };
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (RunningEnvironment?.IsRunning == true)
            {
                RunningEnvironment.Stop();
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
            SaveCurrentConfig();

            var cfg = GetCurrentConfiguration();
            var env = new BotEnvironmentImpl(Hub);
            B_Start.Enabled = false;
            B_Stop.Enabled = true;
            B_New.Enabled = false;
            B_Delete.Enabled = false;

            RunningEnvironment = env;
            LogUtil.LogInfo("Starting", "Form");
            env.Start(cfg);
            Tab_Logs.Select();
        }

        private void B_Stop_Click(object sender, EventArgs e)
        {
            var env = RunningEnvironment;
            if (env == null)
                throw new ArgumentNullException(nameof(RunningEnvironment), "Should have an environment before calling stop!");
            if (!env.CanStop)
                return;

            if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift) // either, because remembering which can be hard
            {
                if (env.IsRunning)
                {
                    env.Pause();
                    WinFormsUtil.Alert("Commanding all bots to Idle.", "Press Stop (without a modifier key) to hard-stop and unlock control.");
                }
                else
                {
                    env.Resume();
                    WinFormsUtil.Alert("Commanding all bots to resume their original task.", "Press Stop (without a modifier key) to hard-stop and unlock control.");
                }
                return;
            }

            env.Stop();
            B_Start.Enabled = true;
            B_Stop.Enabled = false;
            B_New.Enabled = true;
            B_Delete.Enabled = true;
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
            var ip = cfg.GetAddress();
            var match = Bots.FindIndex(z => z.GetAddress().Equals(ip));
            if (match >= 0)
                RemoveBotsAtIndexes(new[] { match });

            Bots.Add(cfg);

            var row = new[] { cfg.IP, cfg.Port.ToString(), cfg.NextRoutineType.ToString(), "Idle" };
            var lvi = new ListViewItem(row);
            LV_Bots.Items.Add(lvi);

            B_Start.Enabled = true;
            B_Delete.Enabled = true;
            return true;
        }

        private void B_Delete_Click(object sender, EventArgs e)
        {
            var indexes = LV_Bots.SelectedIndices;
            var items = indexes.Cast<int>().OrderByDescending(z => z);

            var removed = RemoveBotsAtIndexes(items);
            if (!removed)
            {
                WinFormsUtil.Error("No bots removed.", "Ensure you've selected at least one IP address to remove.");
                return;
            }

            if (Bots.Count == 0)
            {
                B_Start.Enabled = false;
                B_Delete.Enabled = false;
            }

            System.Media.SystemSounds.Asterisk.Play();
        }

        private bool RemoveBotsAtIndexes(IEnumerable<int> items)
        {
            bool removed = false;
            foreach (var item in items)
            {
                Bots.RemoveAt(item);
                LV_Bots.Items.RemoveAt(item);
                removed = true;
            }

            return removed;
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

        private void LV_Bots_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lv = (ListView)sender;
            var index = lv.SelectedIndices.OfType<int>().FirstOrDefault();
            if (index >= Bots.Count)
                return;
            var cfg = Bots[index];
            TB_IP.Text = cfg.IP;
            NUD_Port.Text = cfg.Port.ToString();
            CB_Routine.SelectedIndex = CB_Routine.Items.OfType<ComboItem>().ToList().FindIndex(z => (int)cfg.NextRoutineType == z.Value);
        }
    }
}
