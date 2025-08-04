using Discord;
using Discord.WebSocket;
using System.Linq;

namespace SysBot.Pokemon.Discord;

public static class MedalHelpers
{
    public static int GetCurrentMilestone(int totalTrades)
    {
        int[] milestones = { 1000, 950, 900, 850, 800, 700, 650, 600, 550, 500, 450, 400, 350, 300, 250, 200, 150, 100, 50, 1 };
        return milestones.FirstOrDefault(m => totalTrades >= m, 0);
    }

    public static Embed CreateMedalsEmbed(SocketUser user, int milestone, int totalTrades)
    {
        string status = milestone switch
        {
            1 => "Beginner Trainer",
            50 => "Rookie Trainer",
            100 => "Rising Star",
            150 => "Challenger",
            200 => "Master Baiter",
            250 => "Star Trainer",
            300 => "Ace Trainer",
            350 => "Veteran Trainer",
            400 => "Expert Trainer",
            450 => "Pokémon Trader",
            500 => "Pokémon Professor",
            550 => "Pokémon Champion",
            600 => "Pokémon Specialist",
            650 => "Pokémon Hero",
            700 => "Pokémon Elite",
            750 => "Pokémon Legend",
            800 => "Region Master",
            850 => "Pokémon Master",
            900 => "World Famous",
            950 => "Master Trader",
            1000 => "Pokémon God",
            _ => "New Trainer"
        };

        string description = $"Total Trades: **{totalTrades}**\n**Current Status:** {status}";

        if (milestone > 0)
        {
            string imageUrl = $"https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/Medal/{milestone:D4}.png";
            return new EmbedBuilder()
                .WithTitle($"{user.Username}'s Trading Status")
                .WithColor(new Color(255, 215, 0))
                .WithDescription(description)
                .WithThumbnailUrl(imageUrl)
                .Build();
        }
        else
        {
            string imageUrl = $"https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/Medal/0000.png";
            return new EmbedBuilder()
                .WithTitle($"{user.Username}'s Trading Status")
                .WithColor(new Color(0, 255, 0)) // Lime Green
                .WithDescription($"{description}\nNo trades on record yet, thank you for participating!")
                .WithThumbnailUrl(imageUrl)
                .Build();
        }
    }
}
