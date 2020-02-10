using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon;

namespace SysBot.WinForms
{
    public sealed partial class Main : Form
    {
        private static readonly string WorkingDirectory = Application.StartupPath;
        private static readonly string ConfigPath = Path.Combine(WorkingDirectory, "config.json");
        private readonly List<PokeBotConfig> Bots = new List<PokeBotConfig>();
        private readonly PokeTradeHubConfig Hub;

        private BotEnvironment? RunningEnvironment;

        public Main()
        {
            InitializeComponent();
            MinimumSize = Size;

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllText(ConfigPath);
                var cfg = JsonConvert.DeserializeObject<BotEnvironmentConfig>(lines);
                Bots.AddRange(cfg.Bots);
                Hub = cfg.Hub;
            }
            else
            {
                Hub = new PokeTradeHubConfig();
                Hub.CreateDefaults(WorkingDirectory);
            }
        }

        private BotEnvironmentConfig GetCurrentConfiguration()
        {
            return new BotEnvironmentConfig
            {
                Bots = Bots.ToArray(),
                Hub = Hub,
            };
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            var cfg = GetCurrentConfiguration();
            var lines = JsonConvert.SerializeObject(cfg);
            File.WriteAllText(ConfigPath, lines);
        }

        private void B_Start_Click(object sender, EventArgs e)
        {
            var cfg = GetCurrentConfiguration();
            var env = new BotEnvironment();
            B_Start.Enabled = false;
            B_Stop.Enabled = true;
            B_New.Enabled = false;
            B_Delete.Enabled = false;
            env.Start(cfg);
            RunningEnvironment = env;
        }

        private void B_Stop_Click(object sender, EventArgs e)
        {
            var env = RunningEnvironment;
            if (env == null)
                throw new ArgumentNullException(nameof(RunningEnvironment), "Should have an environment before calling stop!");
            if (!env.CanStop)
                throw new ArgumentOutOfRangeException(nameof(BotEnvironment.CanStop), "Should be running before calling stop!");
            env.Stop();
            B_Start.Enabled = true;
            B_Stop.Enabled = false;
            B_New.Enabled = true;
            B_Delete.Enabled = true;
        }
    }

    public sealed class BotEnvironmentConfig
    {
        public PokeTradeHubConfig Hub { get; set; } = new PokeTradeHubConfig();
        public PokeBotConfig[] Bots { get; set; } = Array.Empty<PokeBotConfig>();
    }

    public sealed class BotEnvironment
    {
        public readonly PokeTradeHub<PK8> Hub = new PokeTradeHub<PK8>();
        private CancellationTokenSource Source = new CancellationTokenSource();
        public List<SwitchBotConfig> Bots = new List<SwitchBotConfig>();

        public bool CanStart => Hub.Bots.Count != 0;
        public bool CanStop => IsRunning;
        public bool IsRunning { get; private set; }

        public void Start(BotEnvironmentConfig cfg)
        {
            Hub.Config = cfg.Hub;
            foreach (var bot in cfg.Bots)
            {

            }
            Source = new CancellationTokenSource();
            IsRunning = true;
        }

        public void Stop()
        {
            Source.Cancel();
            IsRunning = false;
        }
    }
}
