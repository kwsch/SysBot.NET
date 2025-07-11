using Discord.WebSocket;

namespace SysBot.Pokemon.ConsoleApp
{
    internal class CommandHandler
    {
        private DiscordSocketClient discord;

        public CommandHandler(DiscordSocketClient discord)
        {
            this.discord = discord;
        }
    }
}
