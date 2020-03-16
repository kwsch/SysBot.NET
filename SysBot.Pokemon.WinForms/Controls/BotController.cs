using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SysBot.Base;

namespace SysBot.Pokemon.WinForms
{
    public partial class BotController : UserControl
    {
        public PokeBotConfig Config = new PokeBotConfig();
        private PokeBotRunner? Runner;
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

                contextMenuStrip1.Items.Add(item);
            }

            var remove = new ToolStripMenuItem("Remove");
            remove.Click += (_, __) => TryRemove();
            contextMenuStrip1.Items.Add(remove);
            contextMenuStrip1.Opening += ContextMenuStrip1OnOpening;

            var controls = Controls;
            foreach (var c in controls.OfType<Control>())
            {
                c.MouseEnter += BotController_MouseEnter;
                c.MouseLeave += BotController_MouseLeave;
            }
        }

        private void ContextMenuStrip1OnOpening(object sender, CancelEventArgs e)
        {
            if (Runner == null)
                return;

            bool runOnce = Runner.RunOnce;
            var bot = Runner.GetBot(Config);
            if (bot == null)
                return;

            foreach (var tsi in contextMenuStrip1.Items.OfType<ToolStripMenuItem>())
            {
                var text = tsi.Text;
                tsi.Enabled = Enum.TryParse(text, out BotControlCommand cmd)
                    ? runOnce && cmd.IsUsableWhileRunning() == bot.IsRunning
                    : !bot.IsRunning;
            }
        }

        public void Initialize(PokeBotRunner runner, PokeBotConfig cfg)
        {
            Runner = runner;
            Config = cfg;
            ReloadStatus();
            L_Description.Text = string.Empty;
        }

        public void ReloadStatus()
        {
            L_Left.Text = $"{Config.IP}{Environment.NewLine}{Config.InitialRoutine}";
        }

        public void ReloadStatus(SwitchRoutineExecutor<PokeBotConfig> bot)
        {
            ReloadStatus();
            L_Description.Text = $"[{bot.LastTime:hh:mm:ss}] {bot.Connection.Name}: {bot.LastLogged}";
            L_Left.Text = $"{Config.IP}{Environment.NewLine}{Config.InitialRoutine}";

            if (bot.Config.CurrentRoutineType == PokeRoutineType.Idle)
            {
                pictureBox1.BackColor = Color.Yellow;
            }
            else
            {
                var delta = DateTime.Now - bot.LastTime;
                if (delta.Seconds > 100)
                {
                    if (pictureBox1.BackColor == Color.Red)
                        return; // should we notify on change instead?
                    pictureBox1.BackColor = Color.Red;
                }
                else
                {
                    pictureBox1.BackColor = Color.Green;
                }
            }
        }

        public void TryRemove()
        {
            var bot = GetBot();
            if (!Runner!.Hub.Config.SkipConsoleBotCreation)
                bot.Stop();
            Remove?.Invoke(this, EventArgs.Empty);
        }

        public void SendCommand(BotControlCommand index)
        {
            if (Runner?.Hub.Config.SkipConsoleBotCreation != false)
                return;
            var bot = GetBot();
            switch (index)
            {
                case BotControlCommand.Idle: bot.Pause(); break;
                case BotControlCommand.Start: bot.Start(); break;
                case BotControlCommand.Stop: bot.Stop(); break;
                case BotControlCommand.Resume: bot.Resume(); break;
                default:
                    WinFormsUtil.Alert($"{index} is not a command that can be sent to the Bot.");
                    break;
            }
        }

        private BotSource<PokeBotConfig> GetBot()
        {
            if (Runner == null)
                throw new ArgumentNullException(nameof(Runner));

            var bot = Runner.GetBot(Config);
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
                Invoke((MethodInvoker)(() => ReloadStatus(bot.Bot)));
            }
            else
            {
                ReloadStatus(bot.Bot);
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
    }

    public static class BotControlCommandExtensions
    {
        public static bool IsUsableWhileRunning(this BotControlCommand cmd)
        {
            switch (cmd)
            {
                case BotControlCommand.Stop:
                case BotControlCommand.Idle:
                    return true;
                default:
                    return false;
            }
        }
    }
}
