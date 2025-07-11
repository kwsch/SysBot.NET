using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Discord;
using Discord.WebSocket;

namespace SysBot.Pokemon
{
    public class TradeCodeStorage
    {
        private const string FileName = "tradecodes.json";
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private Dictionary<ulong, TradeCodeDetails>? _tradeCodeDetails;

        // Milestone image URLs
        private readonly Dictionary<int, string> _milestoneImages = new()
        {
            { 1, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/001.png" },
            { 50, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/050.png" },
            { 100, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/100.png" },
            { 150, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/150.png" },
            { 200, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/200.png" },
            { 250, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/250.png" },
            { 300, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/300.png" },
            { 350, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/350.png" },
            { 400, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/400.png" },
            { 450, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/450.png" },
            { 500, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/500.png" },
            { 550, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/550.png" },
            { 600, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/600.png" },
            { 650, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/650.png" },
            { 700, "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/700.png" },
            // Add more milestone images...
        };

        public TradeCodeStorage() => LoadFromFile();

        public bool DeleteTradeCode(ulong trainerID)
        {
            LoadFromFile();
            if (_tradeCodeDetails!.Remove(trainerID))
            {
                SaveToFile();
                return true;
            }
            return false;
        }

        public int GetTradeCode(ulong trainerID, ISocketMessageChannel channel, SocketUser user)
        {
            LoadFromFile();
            if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
            {
                details.TradeCount++;
                SaveToFile();

                // Check if trade count is a milestone and send embed
                CheckTradeMilestone(details.TradeCount, channel, user);
                return details.Code;
            }
            var code = GenerateRandomTradeCode();
            _tradeCodeDetails![trainerID] = new TradeCodeDetails { Code = code, TradeCount = 1 };
            SaveToFile();

            // Check for first trade milestone
            CheckTradeMilestone(1, channel, user);
            return code;
        }

        public int GetTradeCount(ulong trainerID)
        {
            LoadFromFile();
            return _tradeCodeDetails!.TryGetValue(trainerID, out var details) ? details.TradeCount : 0;
        }

        public TradeCodeDetails? GetTradeDetails(ulong trainerID)
        {
            LoadFromFile();
            return _tradeCodeDetails!.TryGetValue(trainerID, out var details) ? details : null;
        }

        public void UpdateTradeDetails(ulong trainerID, string ot, int tid, int sid)
        {
            LoadFromFile();
            if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
            {
                details.OT = ot;
                details.TID = tid;
                details.SID = sid;
                SaveToFile();
            }
        }

        public bool UpdateTradeCode(ulong trainerID, int newCode)
        {
            LoadFromFile();
            if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
            {
                details.Code = newCode;
                SaveToFile();
                return true;
            }
            return false;
        }

        private static int GenerateRandomTradeCode()
        {
            var settings = new TradeSettings();
            return settings.GetRandomTradeCode();
        }

        private void CheckTradeMilestone(int tradeCount, ISocketMessageChannel channel, SocketUser user)
        {
            if (_milestoneImages.ContainsKey(tradeCount))
            {
                SendMilestoneEmbed(tradeCount, channel, user);
            }
        }

        private async void SendMilestoneEmbed(int tradeCount, ISocketMessageChannel channel, SocketUser user)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle($"🎉 Congratulations, {user.Username}! 🎉")
                .WithColor(Color.Gold)
                .WithImageUrl(_milestoneImages[tradeCount]);

            embedBuilder.WithDescription(
                tradeCount == 1
                    ? "Congratulations on your very first trade!\nCollect medals by trading with the bot!\nEvery 50 trades is a new medal!\nHow many can you collect?\nSee your current medals with **ml**."
                    : $"You’ve completed {tradeCount} trades!\n*Keep up the great work!*");

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private void LoadFromFile()
        {
            if (File.Exists(FileName))
            {
                string json = File.ReadAllText(FileName);
                _tradeCodeDetails = JsonSerializer.Deserialize<Dictionary<ulong, TradeCodeDetails>>(json, SerializerOptions);
            }
            else
            {
                _tradeCodeDetails = new Dictionary<ulong, TradeCodeDetails>();
            }
        }

        private void SaveToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(_tradeCodeDetails, SerializerOptions);
                File.WriteAllText(FileName, json);
            }
            catch (IOException ex)
            {
                LogUtil.LogInfo("TradeCodeStorage", $"Error saving trade codes to file: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogUtil.LogInfo("TradeCodeStorage", $"Access denied while saving trade codes to file: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogUtil.LogInfo("TradeCodeStorage", $"An error occurred while saving trade codes to file: {ex.Message}");
            }
        }

        public List<string> GetEarnedMedals(ulong trainerID)
        {
            LoadFromFile();

            // Check if user exists and retrieve their trade details
            if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
            {
                var earnedMedals = new List<string>();
                foreach (var milestone in _milestoneImages.Keys)
                {
                    if (details.TradeCount >= milestone)
                    {
                        earnedMedals.Add(_milestoneImages[milestone]);
                    }
                }
                return earnedMedals;
            }

            return new List<string>(); // Return empty if no medals are earned or user not found
        }

        public class TradeCodeDetails
        {
            public int Code { get; set; }
            public string? OT { get; set; }
            public int SID { get; set; }
            public int TID { get; set; }
            public int TradeCount { get; set; }
        }
    }
}
