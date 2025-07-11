using Discord.Commands;
using Discord.WebSocket;
using Discord;
using SysBot.Pokemon;
using System.Threading.Tasks;

public class MedalsModule : ModuleBase<SocketCommandContext>
{
    private readonly TradeCodeStorage _tradeCodeStorage = new TradeCodeStorage();

    private Embed CreateMedalsEmbed(SocketUser user, string medalUrl, int tradeCount)
    {
        string description;
        string imageUrl = medalUrl;

        switch (tradeCount)
        {
            case 1:
                description = "Congratulations on your first trade!\n**Status:** Newbie Trainer.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/001.png";
                break;
            case 50:
                description = "You've reached 50 trades!\n**Status:** Novice Trainer.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/050.png";
                break;
            case 100:
                description = "You've reached 100 trades!\n**Status:** Pokémon Professor.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/100.png";
                break;
            case 150:
                description = "You've reached 150 trades!\n**Status:** Pokémon Specialist.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/150.png";
                break;
            case 200:
                description = "You've reached 200 trades!\n**Status:** Pokémon Champion.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/200.png";
                break;
            case 250:
                description = "You've reached 250 trades!\n**Status:** Pokémon Hero.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/250.png";
                break;
            case 300:
                description = "You've reached 300 trades!\n**Status:** Pokémon Elite.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/300.png";
                break;
            case 350:
                description = "You've reached 350 trades!\n**Status:** Pokémon Trader.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/350.png";
                break;
            case 400:
                description = "You've reached 400 trades!\n**Status:** Pokémon Sage.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/400.png";
                break;
            case 450:
                description = "You've reached 450 trades!\n**Status:** Pokémon Legend.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/450.png";
                break;
            case 500:
                description = "You've reached 500 trades!\n**Status:** Region Master.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/500.png";
                break;
            case 550:
                description = "You've reached 550 trades!\n**Status:** Trade Master.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/550.png";
                break;
            case 600:
                description = "You've reached 600 trades!\n**Status:** World Famous.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/600.png";
                break;
            case 650:
                description = "You've reached 650 trades!\n**Status:** Pokémon Master.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/650.png";
                break;
            case 700:
                description = "You've reached 700 trades!\n**Status:** Pokémon God.";
                imageUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/700.png";
                break;
            default:
                description = $"Congratulations on reaching {tradeCount} trades! Keep it going!";
                break;
        }

        // Create and return the embed with dynamic description and the thumbnail for the medal
        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username}'s Milestone Medal")
            .WithColor(Color.Gold)
            .WithDescription(description)
            .WithThumbnailUrl(imageUrl)
            .Build();

        return embed;
    }

    [Command("medals")]
    [Alias("ml")]
    public async Task ShowMedalsCommand()
    {
        // Fetch earned medal URLs from TradeCodeStorage
        var earnedMedals = _tradeCodeStorage.GetEarnedMedals(Context.User.Id);

        if (earnedMedals.Count == 0)
        {
            await Context.Channel.SendMessageAsync($"{Context.User.Username}, you haven't earned any medals yet. Start trading to earn your first one!");
            return;
        }

        int delayBetweenMedals = 1000; // 1 second delay

        // Loop through each earned medal URL with a dynamic description for each and then send it
        for (int i = 0; i < earnedMedals.Count; i++)
        {
            int tradeCount = GetTradeCountForMedal(i);

            var medalUrl = earnedMedals[i];

            var embed = CreateMedalsEmbed(Context.User, medalUrl, tradeCount);

            await Context.Channel.SendMessageAsync(embed: embed);

            await Task.Delay(delayBetweenMedals);
        }
    }

    // Helper function to get the actual trade counts
    private int GetTradeCountForMedal(int index)
    {
        // match the milestones
        switch (index)
        {
            case 0: return 1;
            case 1: return 50;
            case 2: return 100;
            case 3: return 150;
            case 4: return 200;
            case 5: return 250;
            case 6: return 300;
            case 7: return 350;
            case 8: return 400;
            case 9: return 450;
            case 10: return 500;
            case 11: return 550;
            case 12: return 600;
            case 13: return 650;
            case 14: return 700;
            default: return 0; // Default to 0 for unrecognized indices
        }
    }
}
