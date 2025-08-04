using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class BotModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("botStatus")]
        [Summary("Gets the status of the bots.")]
        [RequireSudo]
        public async Task GetStatusAsync()
        {
            var me = SysCord<T>.Runner;
            var bots = me.Bots.Select(z => z.Bot).OfType<PokeRoutineExecutorBase>().ToArray();
            if (bots.Length == 0)
            {
                await ReplyAsync("No bots configured.").ConfigureAwait(false);
                return;
            }

            var summaries = bots.Select(GetDetailedSummary);
            var lines = string.Join(Environment.NewLine, summaries);
            await ReplyAsync(Format.Code(lines)).ConfigureAwait(false);
        }

        private static string GetBotIPFromJsonConfig()
        {
            try
            {
                // Read the file and parse the JSON
                var jsonData = File.ReadAllText(DudeBot.ConfigPath);
                var config = JObject.Parse(jsonData);

                // Access the IP address from the first bot in the Bots array
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var ip = config["Bots"][0]["Connection"]["IP"].ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                return ip;
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during reading or parsing the file
                Console.WriteLine($"Error reading config file: {ex.Message}");
                return "192.168.1.1"; // Default IP if error occurs
            }
        }

        private static string GetDetailedSummary(PokeRoutineExecutorBase z)
        {
            return $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
        }

        [Command("botStart")]
        [Summary("Starts the currently running bot.")]
        [RequireSudo]
        public async Task StartBotAsync([Summary("IP address of the bot")] string? ip = null)
        {
            if (ip == null)
                ip = BotModule<T>.GetBotIPFromJsonConfig();

            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Start();
            await ReplyAsync("Bot has been started.").ConfigureAwait(false);
        }

        [Command("botStop")]
        [Summary("Stops the currently running bot.")]
        [RequireSudo]
        public async Task StopBotAsync([Summary("IP address of the bot")] string? ip = null)
        {
            if (ip == null)
                ip = BotModule<T>.GetBotIPFromJsonConfig();

            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Stop();
            await ReplyAsync("Bot has been stopped.").ConfigureAwait(false);
        }

        [Command("botIdle")]
        [Alias("botPause")]
        [Summary("Commands the currently running bot to Idle.")]
        [RequireSudo]
        public async Task IdleBotAsync([Summary("IP address of the bot")] string? ip = null)
        {
            if (ip == null)
                ip = BotModule<T>.GetBotIPFromJsonConfig();

            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Pause();
            await ReplyAsync("Bot has been set to idle.").ConfigureAwait(false);
        }

        [Command("botChange")]
        [Summary("Changes the routine of the currently running bot (trades).")]
        [RequireSudo]
        public async Task ChangeTaskAsync([Summary("Routine enum name")] PokeRoutineType task, [Summary("IP address of the bot")] string? ip = null)
        {
            if (ip == null)
                ip = BotModule<T>.GetBotIPFromJsonConfig();

            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Bot.Config.Initialize(task);
            await ReplyAsync($"Bot has changed its routine to {task}.").ConfigureAwait(false);
        }

        [Command("botRestart")]
        [Summary("Restarts the currently running bot(s).")]
        [RequireSudo]
        public async Task RestartBotAsync([Summary("IP address of the bot")] string? ip = null)
        {
            if (ip == null)
                ip = BotModule<T>.GetBotIPFromJsonConfig();

            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
            await ReplyAsync("Bot has been restarted.").ConfigureAwait(false);
        }
    }
}
