using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace SysBot.Pokemon;

public class TradeCodeStorage
{
    private const string FileName = "tradecodes.json";
    private Dictionary<ulong, TradeCodeDetails> _tradeCodeDetails;

    public class TradeCodeDetails
    {
        public int Code { get; set; }
        public string? OT { get; set; }
        public int TID { get; set; }
        public int SID { get; set; }
        public int Language { get; set; } = 2;
        public int TradeCount { get; set; }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TradeCodeStorage()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        LoadFromFile();
    }

    public int GetTradeCode(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details.TradeCount++;
            SaveToFile();
            return details.Code;
        }

        var code = GenerateRandomTradeCode();
        _tradeCodeDetails[trainerID] = new TradeCodeDetails { Code = code, TradeCount = 1 };
        SaveToFile();
        return code;
    }

    private static int GenerateRandomTradeCode()
    {
        var settings = new TradeSettings();
        return settings.GetRandomTradeCode();
    }

    private void LoadFromFile()
    {
        if (File.Exists(FileName))
        {
            string json = File.ReadAllText(FileName);
#pragma warning disable CS8601 // Possible null reference assignment.
            _tradeCodeDetails = JsonSerializer.Deserialize<Dictionary<ulong, TradeCodeDetails>>(json, SerializerOptions);
#pragma warning restore CS8601 // Possible null reference assignment.
        }
        else
        {
            _tradeCodeDetails = new Dictionary<ulong, TradeCodeDetails>();
        }
    }

    public bool DeleteTradeCode(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.Remove(trainerID))
        {
            SaveToFile();
            return true;
        }
        return false;
    }

    private void SaveToFile()
    {
        string json = JsonSerializer.Serialize(_tradeCodeDetails, SerializerOptions);
        File.WriteAllText(FileName, json);
    }

    public int GetTradeCount(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            return details.TradeCount;
        }
        return 0;
    }

    public TradeCodeDetails? GetTradeDetails(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            return details;
        }
        return null;
    }

    public void UpdateTradeDetails(ulong trainerID, string ot, int tid, int sid, int language = 2)
    {
        LoadFromFile();
        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details.OT = ot;
            details.TID = tid;
            details.SID = sid;
            if (language != -1)
                details.Language = language;
            SaveToFile();
        }
    }
}
