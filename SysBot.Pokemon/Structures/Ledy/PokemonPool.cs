using System;
using System.Collections.Generic;
using System.IO;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokemonPool<T> : List<T> where T : PKM, new()
    {
        public readonly int ExpectedSize = new T().Data.Length;

        public readonly IPoolSettings Settings;

        public PokemonPool(IPoolSettings settings)
        {
            Settings = settings;
        }

        public bool Randomized => Settings.DistributeShuffled;

        private int Counter;

        public T GetRandomPoke()
        {
            var choice = this[Counter];
            Counter = (Counter + 1) % Count;
            if (Counter == 0 && Randomized)
                Util.Shuffle(this);
            return choice;
        }

        public bool Reload()
        {
            return LoadFolder(Settings.DistributeFolder);
        }

        public bool LoadFolder(string path)
        {
            Clear();
            if (!Directory.Exists(path))
                return false;

            var loadedAny = false;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            var matchFiles = LoadUtil.GetFilesOfSize(files, ExpectedSize);
            var matchPKM = LoadUtil.GetPKMFilesOfType<T>(matchFiles);

            foreach (var dest in matchPKM)
            {
                if (dest.Species == 0 || !new LegalityAnalysis(dest).Valid || !(dest is PK8 pk8))
                {
                    Console.WriteLine("SKIPPED: Provided pk8 is not valid: " + dest.FileName);
                    continue;
                }
                if (pk8.RibbonClassic || pk8.RibbonPremier || pk8.RibbonBirthday)
                {
                    Console.WriteLine("SKIPPED: Provided pk8 has a special ribbon and can't be Surprise Traded: " + dest.FileName);
                    continue;
                }

                if (Settings.ResetHOMETracker)
                    pk8.Tracker = 0;

                Add(dest);
                loadedAny = true;
            }
            return loadedAny;
        }
    }
}