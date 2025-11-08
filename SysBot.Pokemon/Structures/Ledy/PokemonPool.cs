using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;

namespace SysBot.Pokemon;

public class PokemonPool<T>(BaseConfig Settings) : List<T>
    where T : PKM, new()
{
    private readonly int ExpectedSize = new T().Data.Length;
    private bool Randomized => Settings.Shuffled;

    public readonly Dictionary<string, LedyRequest<T>> Files = [];
    private int Counter;

    public T GetRandomPoke()
    {
        var choice = this[Counter];
        Counter = (Counter + 1) % Count;
        if (Counter == 0 && Randomized)
            Shuffle(this, 0, Count, Util.Rand);
        return choice;
    }

    public static void Shuffle(IList<T> items, int start, int end, Random rnd)
    {
        for (int i = start; i < end; i++)
        {
            int index = i + rnd.Next(end - i);
            (items[index], items[i]) = (items[i], items[index]);
        }
    }

    public T GetRandomSurprise()
    {
        while (true)
        {
            var rand = GetRandomPoke();
            if (DisallowRandomRecipientTrade(rand))
                continue;
            return rand;
        }
    }

    public bool Reload(string path, SearchOption opt = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(path))
            return false;
        Clear();
        Files.Clear();
        return LoadFolder(path, opt);
    }

    public bool LoadFolder(string path, SearchOption opt = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(path))
            return false;

        var loadedAny = false;
        var files = Directory.EnumerateFiles(path, "*", opt);
        var matchFiles = LoadUtil.GetFilesOfSize(files, ExpectedSize);

        int surpriseBlocked = 0;
        foreach (var file in matchFiles)
        {
            var data = File.ReadAllBytes(file);
            var prefer = EntityFileExtension.GetContextFromExtension(file);
            var pkm = EntityFormat.GetFromBytes(data, prefer);
            if (pkm is null)
                continue;
            if (pkm is not T dest)
                continue;

            if (dest.Species == 0)
            {
                LogUtil.LogInfo("SKIPPED: Provided file is not valid: " + dest.FileName, nameof(PokemonPool<T>));
                continue;
            }

            if (!dest.CanBeTraded())
            {
                LogUtil.LogInfo("SKIPPED: Provided file cannot be traded: " + dest.FileName, nameof(PokemonPool<T>));
                continue;
            }

            var la = new LegalityAnalysis(dest);
            if (!la.Valid)
            {
                var reason = la.Report();
                LogUtil.LogInfo($"SKIPPED: Provided file is not legal: {dest.FileName} -- {reason}", nameof(PokemonPool<T>));
                continue;
            }

            if (typeof(T) == typeof(PK8) && DisallowRandomRecipientTrade(dest, la.EncounterMatch))
            {
                LogUtil.LogInfo("Provided file was loaded but can't be Surprise Traded: " + dest.FileName, nameof(PokemonPool<T>));
                surpriseBlocked++;
            }

            if (Settings.Legality.ResetHOMETracker && dest is IHomeTrack h)
                h.Tracker = 0;

            var fn = Path.GetFileNameWithoutExtension(file);
            fn = StringsUtil.Sanitize(fn);

            // Since file names can be sanitized to the same string, only add one of them.
            if (!Files.ContainsKey(fn))
            {
                Add(dest);
                Files.Add(fn, new LedyRequest<T>(dest, fn));
            }
            else
            {
                LogUtil.LogInfo("Provided file was not added due to duplicate name: " + dest.FileName, nameof(PokemonPool<T>));
            }
            loadedAny = true;
        }

        if (typeof(T) == typeof(PK8) && surpriseBlocked == Count)
            LogUtil.LogInfo("Surprise trading will fail; failed to load any compatible files.", nameof(PokemonPool<T>));

        return loadedAny;
    }

    private static bool DisallowRandomRecipientTrade(T pk, IEncounterTemplate enc)
    {
        // Anti-spam
        if (pk.IsNicknamed)
        {
            Span<char> nick = stackalloc char[pk.TrashCharCountNickname];
            int len = pk.LoadString(pk.NicknameTrash, nick);
            if (len > 6 && enc is not IFixedNickname { IsFixedNickname: true })
                return true;
            nick = nick[..len];
            if (StringsUtil.IsSpammyString(nick))
                return true;
        }
        {
            Span<char> ot = stackalloc char[pk.TrashCharCountTrainer];
            int len = pk.LoadString(pk.OriginalTrainerTrash, ot);
            ot = ot[..len];
            if (StringsUtil.IsSpammyString(ot) && !AutoLegalityWrapper.IsFixedOT(enc, pk))
                return true;
        }

        return DisallowRandomRecipientTrade(pk);
    }

    public static bool DisallowRandomRecipientTrade(T pk)
    {
        // Surprise Trade currently bans Mythicals and Legendaries, not Sub-Legendaries.
        if (SpeciesCategory.IsLegendary(pk.Species))
            return true;
        if (SpeciesCategory.IsMythical(pk.Species))
            return true;

        // Can't surprise trade fused stuff.
        if (FormInfo.IsFusedForm(pk.Species, pk.Form, pk.Format))
            return true;

        return false;
    }
}
