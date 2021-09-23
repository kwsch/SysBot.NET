﻿using SysBot.Base;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    public partial class BotController : UserControl
    {
        public PokeBotState State { get; private set; } = new();
        private IPokeBotRunner? Runner;
        public EventHandler? Remove;

        public BotController()
        {
            InitializeComponent();
            var opt = (BotControlCommand[])Enum.GetValues(typeof(BotControlCommand));

            for (int i = 1; i < opt.Length; i++)
            {
                var cmd = opt[i];
                var item = new ToolStripMenuItem(cmd.ToString());
                item.Click += (_, __) => SendCommand(cmd);

                RCMenu.Items.Add(item);
            }

            var remove = new ToolStripMenuItem("Remove");
            remove.Click += (_, __) => TryRemove();
            RCMenu.Items.Add(remove);
            RCMenu.Opening += RcMenuOnOpening;

            var controls = Controls;
            foreach (var c in controls.OfType<Control>())
            {
                c.MouseEnter += BotController_MouseEnter;
                c.MouseLeave += BotController_MouseLeave;
            }
        }

        private void RcMenuOnOpening(object sender, CancelEventArgs e)
        {
            if (Runner == null)
                return;

            bool runOnce = Runner.RunOnce;
            var bot = Runner.GetBot(State);
            if (bot is null)
                return;

            foreach (var tsi in RCMenu.Items.OfType<ToolStripMenuItem>())
            {
                var text = tsi.Text;
                tsi.Enabled = Enum.TryParse(text, out BotControlCommand cmd)
                    ? runOnce && cmd.IsUsable(bot.IsRunning, bot.IsPaused)
                    : !bot.IsRunning;
            }
        }

        public void Initialize(IPokeBotRunner runner, PokeBotState cfg)
        {
            Runner = runner;
            State = cfg;
            ReloadStatus();
            L_Description.Text = string.Empty;
        }

        public void ReloadStatus()
        {
            var bot = GetBot().Bot;
            L_Left.Text = $"{bot.Connection.Name}{Environment.NewLine}{State.InitialRoutine}";
        }

        private DateTime LastUpdateStatus = DateTime.Now;

        public void ReloadStatus(BotSource<PokeBotState> b)
        {
            ReloadStatus();
            var bot = b.Bot;
            L_Description.Text = $"[{bot.LastTime:hh:mm:ss}] {bot.Connection.Label}: {bot.LastLogged}";
            L_Left.Text = $"{bot.Connection.Name}{Environment.NewLine}{State.InitialRoutine}";

            var lastTime = bot.LastTime;
            if (!b.IsRunning)
            {
                PB_Lamp.BackColor = Color.Transparent;
                return;
            }

            var cfg = bot.Config;
            if (cfg.CurrentRoutineType == PokeRoutineType.Idle && cfg.NextRoutineType == PokeRoutineType.Idle)
            {
                PB_Lamp.BackColor = Color.Yellow;
                return;
            }
            if (LastUpdateStatus == lastTime)
                return;

            // Color decay from Green based on time
            const int threshold = 100;
            Color good = Color.Green;
            Color bad = Color.Red;

            var delta = DateTime.Now - lastTime;
            var seconds = delta.Seconds;

            LastUpdateStatus = lastTime;
            if (seconds > 2 * threshold)
                return; // already changed by now

            if (seconds > threshold)
            {
                if (PB_Lamp.BackColor == bad)
                    return; // should we notify on change instead?
                PB_Lamp.BackColor = bad;
            }
            else
            {
                // blend from green->red, favoring green until near saturation
                var factor = seconds / (double)threshold;
                var blend = Blend(bad, good, factor * factor);
                PB_Lamp.BackColor = blend;
            }
        }

        private static Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + (backColor.R * (1 - amount)));
            byte g = (byte)((color.G * amount) + (backColor.G * (1 - amount)));
            byte b = (byte)((color.B * amount) + (backColor.B * (1 - amount)));
            return Color.FromArgb(r, g, b);
        }

        public void TryRemove()
        {
            var bot = GetBot();
            if (!Runner!.Config.SkipConsoleBotCreation)
                bot.Stop();
            Remove?.Invoke(this, EventArgs.Empty);
        }

        public void SendCommand(BotControlCommand cmd, bool echo = true)
        {
            if (Runner?.Config.SkipConsoleBotCreation != false)
            {
                LogUtil.LogError("No bots were created because SkipConsoleBotCreation is on!", "Hub");
                return;
            }
            var bot = GetBot();
            switch (cmd)
            {
                case BotControlCommand.Idle: bot.Pause(); break;
                case BotControlCommand.Start: bot.Start(); break;
                case BotControlCommand.Stop: bot.Stop(); break;
                case BotControlCommand.Resume: bot.Resume(); break;
                case BotControlCommand.Restart:
                {
                    var prompt = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Are you sure you want to restart the connection?");
                    if (prompt != DialogResult.Yes)
                        return;

                    bot.Bot.Connection.Reset();
                    bot.Start();
                    break;
                }
                default:
                    WinFormsUtil.Alert($"{cmd} is not a command that can be sent to the Bot.");
                    return;
            }
            if (echo)
                EchoUtil.Echo($"{bot.Bot.Connection.Name} ({bot.Bot.Config.InitialRoutine}) has been issued a command to {cmd}.");
        }

        private BotSource<PokeBotState> GetBot()
        {
            if (Runner == null)
                throw new ArgumentNullException(nameof(Runner));

            var bot = Runner.GetBot(State);
            if (bot == null)
                throw new ArgumentNullException(nameof(bot));
            return bot;
        }

        private void BotController_MouseEnter(object? sender, EventArgs e) => BackColor = Color.LightSkyBlue;
        private void BotController_MouseLeave(object? sender, EventArgs e) => BackColor = Color.Transparent;

        public void ReadState()
        {
            var bot = GetBot();

            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => ReloadStatus(bot)));
            }
            else
            {
                ReloadStatus(bot);
            }
        }
    }

    public enum BotControlCommand
    {
        None,
        Start,
        Stop,
        Idle,
        Resume,
        Restart,
    }

    public static class BotControlCommandExtensions
    {
        public static bool IsUsable(this BotControlCommand cmd, bool running, bool paused)
        {
            return cmd switch
            {
                BotControlCommand.Start => !running,
                BotControlCommand.Stop => running,
                BotControlCommand.Idle => running && !paused,
                BotControlCommand.Resume => paused,
                BotControlCommand.Restart => true,
                _ => false,
            };
        }
    }
}
