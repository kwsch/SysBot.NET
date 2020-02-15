using System;
using System.Collections.Generic;
using System.IO;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokemonPool<T> : List<T> where T : PKM
    {
        public int ExpectedSize = 344;

        public T GetRandomPoke()
        {
            var rnd = Util.Rand;
            var choice = rnd.Next(Count);
            return this[choice];
        }

        public bool LoadFolder(string path)
        {
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

                Add(dest);
                loadedAny = true;
            }
            return loadedAny;
        }
    }
}