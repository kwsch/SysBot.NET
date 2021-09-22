﻿using SysBot.Base;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokemonPool<T> : List<T> where T : PKM, new()
    {
        private readonly int ExpectedSize = new T().Data.Length;
        private readonly BaseConfig Settings;
        private bool Randomized => Settings.Shuffled;

        public readonly Dictionary<string, LedyRequest<T>> Files = new();
        private int Counter;

        public PokemonPool(BaseConfig settings) => Settings = settings;

        public T GetRandomPoke()
        {
            var choice = this[Counter];
            Counter = (Counter + 1) % Count;
            if (Counter == 0 && Randomized)
                Util.Shuffle(this);
            return choice;
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
                var pkm = PKMConverter.GetPKMfromBytes(data);
                if (pkm is null)
                    continue;
                if (pkm is not T)
                    pkm = PKMConverter.ConvertToType(pkm, typeof(T), out _);
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

                if (DisallowRandomRecipientTrade(dest, la.EncounterMatch))
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

            // Anti-spam: Same trainer names.
            if (Files.Count != 1 && Files.Select(z => z.Value.RequestInfo.OT_Name).Distinct().Count() == 1)
            {
                LogUtil.LogInfo("Provided pool to distribute has the same OT for all loaded. Pool is not valid; please distribute from a variety of trainers.", nameof(PokemonPool<T>));
                surpriseBlocked = Count;
                Files.Clear();
            }

            if (surpriseBlocked == Count)
                LogUtil.LogInfo("Surprise trading will fail; failed to load any compatible files.", nameof(PokemonPool<T>));

            return loadedAny;
        }

        private static bool DisallowRandomRecipientTrade(T pk, IEncounterTemplate enc)
        {
            // Anti-spam
            if (pk.IsNicknamed && enc is not EncounterTrade {IsNicknamed: true} && pk.Nickname.Length > 6)
                return true;
            return DisallowRandomRecipientTrade(pk);
        }

        public static bool DisallowRandomRecipientTrade(T pk)
        {
            // Anti-spam
            if (StringsUtil.IsSpammyString(pk.OT_Name))
                return true;
            if (pk.IsNicknamed && StringsUtil.IsSpammyString(pk.Nickname))
                return true;

            // Surprise Trade currently bans Mythicals and Legendaries, not Sub-Legendaries.
            if (Legal.Legends.Contains(pk.Species))
                return true;

            // Can't surprise trade fused stuff.
            if (FormInfo.IsFusedForm(pk.Species, pk.Form, pk.Format))
                return true;

            return false;
        }
    }
}
