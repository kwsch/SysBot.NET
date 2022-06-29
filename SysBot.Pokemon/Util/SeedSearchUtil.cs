using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public static class SeedSearchUtil
    {
        public static uint GetShinyXor(uint val) => (val >> 16) ^ (val & 0xFFFF);

        public static uint GetShinyType(uint pid, uint tidsid)
        {
            var p = GetShinyXor(pid);
            var t = GetShinyXor(tidsid);
            if (p == t)
                return 2; // square;
            if ((p ^ t) < 0x10)
                return 1; // star
            return 0;
        }

        public static void GetShinyFrames(ulong seed, out int[] frames, out uint[] type, out List<uint[,]> IVs, SeedCheckResults mode)
        {
            int shinyindex = 0;
            frames = new int[3];
            type = new uint[3];
            IVs = new List<uint[,]>();
            bool foundStar = false;
            bool foundSquare = false;

            var rng = new Xoroshiro128Plus(seed);
            for (int i = 0; ; i++)
            {
                rng.NextInt(); // EC
                uint SIDTID = (uint)rng.NextInt();
                uint PID = (uint)rng.NextInt();
                var shinytype = GetShinyType(PID, SIDTID);

                // If we found a shiny, record it and return if we got everything we wanted.
                if (shinytype != 0)
                {
                    if (shinytype == 1)
                        foundStar = true;
                    else if (shinytype == 2)
                        foundSquare = true;

                    if (shinyindex == 0 || mode == SeedCheckResults.FirstThree || (foundStar && foundSquare))
                    {
                        frames[shinyindex] = i;
                        type[shinyindex] = shinytype;
                        GetShinyIVs(rng, out uint[,] frameIVs);
                        IVs.Add(frameIVs);

                        shinyindex++;
                    }

                    if (mode == SeedCheckResults.ClosestOnly || (mode == SeedCheckResults.FirstStarAndSquare && foundStar && foundSquare) || shinyindex >= 3)
                        return;
                }

                // Get the next seed, and reset for the next iteration
                rng = new Xoroshiro128Plus(seed);
                seed = rng.Next();
                rng = new Xoroshiro128Plus(seed);
            }
        }

        public static void GetShinyIVs(Xoroshiro128Plus rng, out uint[,] frameIVs)
        {
            frameIVs = new uint[5, 6];
            Xoroshiro128Plus origrng = rng;

            for (int ivcount = 0; ivcount < 5; ivcount++)
            {
                int i = 0;
                int[] ivs = { -1, -1, -1, -1, -1, -1 };

                while (i < ivcount + 1)
                {
                    var stat = (int)rng.NextInt(6);
                    if (ivs[stat] == -1)
                    {
                        ivs[stat] = 31;
                        i++;
                    }
                }

                for (int j = 0; j < 6; j++)
                {
                    if (ivs[j] == -1)
                        ivs[j] = (int)rng.NextInt(32);
                    frameIVs[ivcount, j] = (uint)ivs[j];
                }
                rng = origrng;
            }
        }
    }
}
