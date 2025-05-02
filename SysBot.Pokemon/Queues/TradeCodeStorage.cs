using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SysBot.Pokemon;

public class TradeCodeStorage
{
    private const string FileName = "tradecodes.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private Dictionary<ulong, TradeCodeDetails>? _tradeCodeDetails;

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

    public int GetTradeCode(ulong trainerID)
    {
        LoadFromFile();
        if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
        {
            details.TradeCount++;
            SaveToFile();
            return details.Code;
        }
        var code = GenerateRandomTradeCode();
        _tradeCodeDetails![trainerID] = new TradeCodeDetails { Code = code, TradeCount = 1 };
        SaveToFile();
        return code;
    }

    public int GetTradeCount(ulong trainerID)
    {
        LoadFromFile();
        if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
        {
            return details.TradeCount;
        }
        return 0;
    }

    public TradeCodeDetails? GetTradeDetails(ulong trainerID)
    {
        LoadFromFile();
        if (_tradeCodeDetails!.TryGetValue(trainerID, out var details))
        {
            return details;
        }
        return null;
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

    public class TradeCodeDetails
    {
        public int Code { get; set; }
        public string? OT { get; set; }
        public int SID { get; set; }
        public int TID { get; set; }
        public int TradeCount { get; set; }
    }
}
