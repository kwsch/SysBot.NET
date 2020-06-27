using System.Collections.Generic;
using System.IO;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokemonPool<T> : List<T> where T : PKM, new()
    {
        public readonly int ExpectedSize = new T().Data.Length;

        public readonly PokeTradeHubConfig Settings;

        public PokemonPool(PokeTradeHubConfig settings)
        {
            Settings = settings;
        }

        public bool Randomized => Settings.Distribute.Shuffled;

        private int Counter;

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
            int ctr = 0;
            while (true)
            {
                var rand = GetRandomPoke();
                if (rand is PK8 pk8 && DisallowSurpriseTrade(pk8))
                    continue;

                ctr++; // if the pool has no valid matches, yield out eventually
                if (ctr > Count * 2)
                    return rand;
            }
        }

        public bool Reload()
        {
            return LoadFolder(Settings.Folder.DistributeFolder);
        }

        public readonly Dictionary<string, LedyRequest<T>> Files = new Dictionary<string, LedyRequest<T>>();

        public bool LoadFolder(string path)
        {
            Clear();
            Files.Clear();
            if (!Directory.Exists(path))
                return false;

            var loadedAny = false;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            var matchFiles = LoadUtil.GetFilesOfSize(files, ExpectedSize);

            int surpriseBlocked = 0;
            foreach (var file in matchFiles)
            {
                var data = File.ReadAllBytes(file);
                var pkm = PKMConverter.GetPKMfromBytes(data);
                if (!(pkm is T dest))
                    continue;

                if (dest.Species == 0 || !(dest is PK8 pk8))
                {
                    LogUtil.LogInfo("SKIPPED: Provided pk8 is not valid: " + dest.FileName, nameof(PokemonPool<T>));
                    continue;
                }

                if (!dest.CanBeTraded())
                {
                    LogUtil.LogInfo("SKIPPED: Provided pk8 cannot be traded: " + dest.FileName, nameof(PokemonPool<T>));
                    continue;
                }

                var la = new LegalityAnalysis(pk8);
                if (!la.Valid && Settings.Legality.VerifyLegality)
                {
                    var reason = la.Report();
                    LogUtil.LogInfo($"SKIPPED: Provided pk8 is not legal: {dest.FileName} -- {reason}", nameof(PokemonPool<T>));
                    continue;
                }

                if (DisallowSurpriseTrade(pk8))
                {
                    LogUtil.LogInfo("Provided pk8 was loaded but can't be Surprise Traded: " + dest.FileName, nameof(PokemonPool<T>));
                    surpriseBlocked++;
                }

                if (Settings.Legality.ResetHOMETracker)
                    pk8.Tracker = 0;

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
                    LogUtil.LogInfo("Provided pk8 was not added due to duplicate name: " + dest.FileName, nameof(PokemonPool<T>));
                }
                loadedAny = true;
            }

            if (surpriseBlocked == Count)
                LogUtil.LogInfo("Surprise trading will fail; failed to load any compatible files.", nameof(PokemonPool<T>));

            return loadedAny;
        }

        private static bool DisallowSurpriseTrade(PK8 pk8)
        {
            // Surprise Trade currently bans Mythicals and Legendaries, not Sub-Legendaries.
            return Legal.Legends.Contains(pk8.Species);
        }
    }
}