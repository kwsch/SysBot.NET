using PKHeX.Core;

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

        public static int GetNextShinyFrame(ulong seed, out uint type)
        {
            var rng = new Xoroshiro128Plus(seed);
            for (int i = 0; ; i++)
            {
                uint _ = (uint)rng.NextInt(0xFFFFFFFF); // EC
                uint SIDTID = (uint)rng.NextInt(0xFFFFFFFF);
                uint PID = (uint)rng.NextInt(0xFFFFFFFF);
                type = GetShinyType(PID, SIDTID);
                if (type != 0)
                    return i;

                // Get the next seed, and reset for the next iteration
                rng = new Xoroshiro128Plus(seed);
                seed = rng.Next();
                rng = new Xoroshiro128Plus(seed);
            }
        }

        public static bool IsMatch(ulong seed, int[] ivs, int fixed_ivs)
        {
            var rng = new Xoroshiro128Plus(seed);
            rng.NextInt(); // EC
            rng.NextInt(); // TID
            rng.NextInt(); // PID
            int[] check_ivs = { -1, -1, -1, -1, -1, -1 };
            for (int i = 0; i < fixed_ivs; i++)
            {
                uint slot;
                do
                {
                    slot = (uint)rng.NextInt(6);
                } while (check_ivs[slot] != -1);

                if (ivs[slot] != 31)
                    return false;

                check_ivs[slot] = 31;
            }
            for (int i = 0; i < 6; i++)
            {
                if (check_ivs[i] != -1)
                    continue; // already verified?

                uint iv = (uint)rng.NextInt(32);
                if (iv != ivs[i])
                    return false;
            }
            return true;
        }
    }
}