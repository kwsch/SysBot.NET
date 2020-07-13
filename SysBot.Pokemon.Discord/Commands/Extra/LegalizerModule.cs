using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class LegalizerModule : ModuleBase<SocketCommandContext>
    {
        [Command("legalize"), Alias("alm")]
        [Summary("Tries to legalize the attached pkm data.")]
        public async Task LegalizeAsync()
        {
            var attachments = Context.Message.Attachments;
            foreach (var att in attachments)
                await Context.Channel.ReplyWithLegalizedSetAsync(att).ConfigureAwait(false);
        }

        [Command("convert"), Alias("showdown")]
        [Summary("Tries to convert the Showdown Set to pkm data.")]
        [Priority(1)]
        public async Task ConvertShowdown([Summary("Generation/Format")] int gen, [Remainder][Summary("Showdown Set")] string content)
        {
            await Context.Channel.ReplyWithLegalizedSetAsync(content, gen).ConfigureAwait(false);
        }

        [Command("convert"), Alias("showdown")]
        [Summary("Tries to convert the Showdown Set to pkm data.")]
        [Priority(0)]
        public async Task ConvertShowdown([Remainder][Summary("Showdown Set")] string content)
        {
            await Context.Channel.ReplyWithLegalizedSetAsync(content).ConfigureAwait(false);
        }
    }
}