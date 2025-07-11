using Discord;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class EmbedHelper
{
    public static async Task SendTradeCodeEmbedAsync(IUser user, int code)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Here's your Trade Code!")
            .WithDescription($"# {code:0000 0000}\n*I'll notify you when your trade starts!*")
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/dm-tradecode.gif")
            .WithColor(Color.Gold)
            .Build();

        await user.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public static async Task SendTradeInitializingEmbedAsync(IUser user, string speciesName, int code, bool isMysteryEgg, string? message = null)
    {
        if (isMysteryEgg)
        {
            speciesName = "**Mystery Egg**";
        }
        var embed = new EmbedBuilder()
            .WithTitle("Loading Trade Menu...")
            .WithDescription($"**Trade Code**: {code:0000 0000}")
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/dm-initializingbot.gif")
            .WithColor(Color.Green);

        if (!string.IsNullOrEmpty(message))
        {
            embed.WithDescription($"{embed.Description}\n\n{message}");
        }

        var builtEmbed = embed.Build();
        await user.SendMessageAsync(embed: builtEmbed).ConfigureAwait(false);
    }

    public static async Task SendTradeSearchingEmbedAsync(IUser user, string trainerName, string inGameName, string? message = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Now Searching...")
            .WithDescription($"**Waiting For**: {trainerName}\n**My IGN**: {inGameName}\n\n**Insert your Trade Code!**")
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/dm-nowsearching.gif")
            .WithColor(Color.DarkGreen);

        if (!string.IsNullOrEmpty(message))
        {
            embed.WithDescription($"{embed.Description}\n\n{message}");
        }

        var builtEmbed = embed.Build();
        await user.SendMessageAsync(embed: builtEmbed).ConfigureAwait(false);
    }

    public static async Task SendNotificationEmbedAsync(IUser user, string message)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Notice...")
            .WithDescription(message)
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/dm-legalityerror.gif")
            .WithColor(Color.Red)
            .Build();

        await user.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public static async Task SendTradeCanceledEmbedAsync(IUser user, string reason)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Uh-Oh...")
            .WithDescription($"Sorry, but there was an error\n**Reason**: {reason}")
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/dm-uhoherror.gif")
            .WithColor(Color.Red)
            .Build();

        await user.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }

    public static async Task SendTradeFinishedEmbedAsync<T>(IUser user, string message, T pk, bool isMysteryEgg)
        where T : PKM, new()
    {
        string speciesImageUrl = AbstractTrade<T>.PokeImg(pk, false, true, null);

        var embed = new EmbedBuilder()
            .WithTitle("Trade Completed!")
            .WithDescription(message)
            .WithTimestamp(DateTimeOffset.Now)
            .WithThumbnailUrl(speciesImageUrl)
            .WithColor(Color.Teal)
            .Build();

        await user.SendMessageAsync(embed: embed).ConfigureAwait(false);
    }
}
