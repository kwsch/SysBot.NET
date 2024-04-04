using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace SysBot.Pokemon;

public class TradeCodeStorage
{
    private const string FileName = "tradecodes.json";
    private Dictionary<ulong, int> _tradeCodes;

    public TradeCodeStorage()
    {
        _tradeCodes = LoadFromFile();
    }

    public int GetTradeCode(ulong trainerID)
    {
        // Load the trade codes from the JSON file
        _tradeCodes = LoadFromFile();

        if (_tradeCodes.TryGetValue(trainerID, out int code))
            return code;

        code = GenerateRandomTradeCode();
        _tradeCodes[trainerID] = code;
        SaveToFile();
        return code;
    }

    private static int GenerateRandomTradeCode()
    {
        var settings = new TradeSettings();
        return settings.GetRandomTradeCode();
    }

    private static Dictionary<ulong, int> LoadFromFile()
    {
        if (File.Exists(FileName))
        {
            string json = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json);
        }
        return [];
    }

    public bool DeleteTradeCode(ulong trainerID)
    {
        // Load the trade codes from the JSON file
        _tradeCodes = LoadFromFile();

        if (_tradeCodes.Remove(trainerID))
        {
            SaveToFile();
            return true;
        }
        return false;
    }

    private void SaveToFile()
    {
        string json = JsonConvert.SerializeObject(_tradeCodes);
        File.WriteAllText(FileName, json);
    }
}
