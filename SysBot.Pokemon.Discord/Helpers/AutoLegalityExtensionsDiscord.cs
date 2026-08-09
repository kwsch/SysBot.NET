using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

public static class AutoLegalityExtensionsDiscord
{
    extension(SocketInteractionContext context)
    {
        public async Task ReplyWithLegalizedSetAsync(ITrainerInfo sav, ShowdownSet set)
        {
            if (set.Species == 0)
            {
                await context.Interaction.FollowupAsync("Oops! I wasn't able to interpret your message! If you intended to convert something, please double check what you're pasting!").ConfigureAwait(false);
                return;
            }

            try
            {
                var template = AutoLegalityWrapper.GetTemplate(set);
                var pk = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pk);
                var species = GameInfo.Strings.Species[template.Species];
                if (!la.Valid)
                {
                    var reason = result switch
                    {
                        "Timeout" => $"That {species} set took too long to generate.",
                        "VersionMismatch" => "Request refused: PKHeX and Auto-Legality Mod version mismatch.",
                        _ => $"I wasn't able to create a {species} from that set.",
                    };
                    var issue = $"Oops! {reason}";
                    if (result == "Failed")
                        issue += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pk)}";

                    await context.Interaction.FollowupAsync(issue).ConfigureAwait(false);
                    return;
                }

                var message = $"Here's your ({result}) legalized PKM for {species} ({la.EncounterOriginal.Name})!";
                var formatted = ReusableActions.GetFormattedShowdownText(pk);
                await context.SendFileAsync(pk, $"{message}\n{formatted}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex);
                var formatted = ReusableActions.FormatSetCode(set);
                var message = $"Oops! An unexpected problem happened with this Showdown Set:\n{formatted}";
                // No need for everyone to see their goofy set.
                await context.Interaction.FollowupAsync(message, ephemeral: true).ConfigureAwait(false);
            }
        }

        public async Task ReplyWithLegalizedSetAsync(string content, GameVersion version)
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var tr = AutoLegalityWrapper.GetTrainerInfo(version);
            await context.ReplyWithLegalizedSetAsync(tr, set).ConfigureAwait(false);
        }

        public async Task ReplyWithLegalizedSetAsync<T>(string content) where T : PKM, new()
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var tr = AutoLegalityWrapper.GetTrainerInfo<T>();
            await context.ReplyWithLegalizedSetAsync(tr, set).ConfigureAwait(false);
        }

        public async Task ReplyWithLegalizedSetAsync(IAttachment attachment)
        {
            var download = await attachment.DownloadEntityAsync().ConfigureAwait(false);
            if (!download.Success)
            {
                await context.Interaction.FollowupAsync(download.ErrorMessage, ephemeral: true).ConfigureAwait(false);
                return;
            }

            var pk = download.Data!;
            var fileName = download.SanitizedFileName;
            if (new LegalityAnalysis(pk).Valid)
            {
                await context.Interaction.FollowupAsync($"{fileName}: Already legal.", ephemeral: true).ConfigureAwait(false);
                return;
            }

            var legal = pk.LegalizePokemon();
            if (!new LegalityAnalysis(legal).Valid)
            {
                await context.Interaction.FollowupAsync($"{fileName}: Unable to legalize.").ConfigureAwait(false);
                return;
            }

            legal.RefreshChecksum();

            var paste = ReusableActions.GetFormattedShowdownText(legal);
            var message = $"Here's your legalized PKM for {fileName}!\n{paste}";
            await context.SendFileAsync(legal, message).ConfigureAwait(false);
        }
    }
}
