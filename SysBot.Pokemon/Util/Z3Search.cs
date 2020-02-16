using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Z3;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class Z3Search
    {
        public static Z3SeedResult GetFirstSeed(uint ec, uint pid, int[] ivs)
        {
            var seeds = GetSeeds(ec, pid);
            bool hasClosest = false;
            ulong closest = 0;
            foreach (var seed in seeds)
            {
                // Verify the IVs; at most 5 can match
                for (int i = 1; i <= 5; i++) // fixed IV count
                {
                    if (IsMatch(seed, ivs, i))
                        return new Z3SeedResult(Z3SearchResult.Success, seed, i);
                }
                hasClosest = true;
                closest = seed;
            }

            if (hasClosest)
                return new Z3SeedResult(Z3SearchResult.SeedMismatch, closest, 0);
            return Z3SeedResult.None;
        }

        public static IList<Z3SeedResult> GetAllSeeds(uint ec, uint pid, int[] ivs)
        {
            var result = new List<Z3SeedResult>();
            var seeds = GetSeeds(ec, pid);
            foreach (var seed in seeds)
            {
                // Verify the IVs; at most 5 can match
                bool added = false;
                for (int i = 1; i <= 5; i++) // fixed IV count
                {
                    if (IsMatch(seed, ivs, i))
                    {
                        result.Add(new Z3SeedResult(Z3SearchResult.Success, seed, i));
                        added = true;
                    }
                }
                if (!added)
                    result.Add(new Z3SeedResult(Z3SearchResult.SeedMismatch, seed, 0));
            }

            if (result.Count == 0)
                result.Add(Z3SeedResult.None);
            return result;
        }

        public static IEnumerable<ulong> GetSeeds(uint ec, uint pid)
        {
            foreach (var seed in FindPotentialSeeds(ec, pid, false))
                yield return seed;
            foreach (var seed in FindPotentialSeeds(ec, pid ^ 0x10000000, false))
                yield return seed;
        }

        public static IEnumerable<ulong> FindPotentialSeeds(uint ec, uint pid, bool shiny)
        {
            using var ctx = new Context(new Dictionary<string, string> { { "model", "true" } });
            var exp = CreateModel(ctx, ec, pid, shiny, out var s0);

            Model? m;
            while ((m = Check(ctx, exp)) != null)
            {
                foreach (var kvp in m.Consts)
                {
                    var tmp = (BitVecNum)kvp.Value;
                    yield return tmp.UInt64; // TODO: Something nicer
                    exp = ctx.MkAnd(exp, ctx.MkNot(ctx.MkEq(s0, m.Evaluate(s0))));
                }
            }
        }

        private static BoolExpr CreateModel(Context ctx, uint ec, uint pid, bool shiny, out BitVecExpr s0)
        {
            s0 = ctx.MkBVConst("s0", 64);
            BitVecExpr s1 = ctx.MkBV(Xoroshiro128Plus.XOROSHIRO_CONST, 64);

            var and_val = ctx.MkBV(0xFFFFFFFF, 64);
            var and_val16 = ctx.MkBV(0xFFFF, 64);
            var bit16 = ctx.MkBV(1 << 16, 64);
            var comp_with = ctx.MkBV(0xF, 64);

            var real_ec = ctx.MkBV(ec, 64);
            var real_pid = ctx.MkBV(pid, 64);

            var ec_check = AdvanceSymbolic(ctx, ref s0, ref s1);
            var tidsid = AdvanceSymbolic(ctx, ref s0, ref s1);
            var pid_check = AdvanceSymbolic(ctx, ref s0, ref s1);

            var exp = ctx.MkEq(ec_check, real_ec);

            if (shiny)
            {
                exp = ctx.MkAnd(exp, ctx.MkEq(ctx.MkBVAND(pid_check, and_val16), ctx.MkBVAND(real_pid, and_val16)));
                var st = ctx.MkBVAND(tidsid, and_val);
                var tsv = ctx.MkBVXOR(ctx.MkBVUDiv(st, bit16), ctx.MkBVAND(st, and_val16));
                var psv = ctx.MkBVXOR(ctx.MkBVUDiv(pid_check, bit16), ctx.MkBVAND(pid_check, and_val16));
                return ctx.MkAnd(exp, ctx.MkBVSLE(ctx.MkBVXOR(tsv, psv), comp_with));
            }
            else
            {
                return ctx.MkAnd(exp, ctx.MkEq(pid_check, real_pid));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BitVecExpr AdvanceSymbolic(Context ctx, ref BitVecExpr s0, ref BitVecExpr s1)
        {
            var and_val = ctx.MkBV(0xFFFFFFFF, 64);
            var res = ctx.MkBVAND(ctx.MkBVAdd(s0, s1), and_val);
            s1 = ctx.MkBVXOR(s0, s1);
            var tmp = ctx.MkBVRotateLeft(24, s0);
            var tmp2 = ctx.MkBV(1 << 16, 64);
            s0 = ctx.MkBVXOR(tmp, ctx.MkBVXOR(s1, ctx.MkBVMul(s1, tmp2)));
            s1 = ctx.MkBVRotateLeft(37, s1);
            return res;
        }

        private static Model? Check(Context ctx, BoolExpr cond)
        {
            Solver solver = ctx.MkSolver();
            solver.Assert(cond);
            Status q = solver.Check();
            if (q != Status.SATISFIABLE)
                return null;
            return solver.Model;
        }

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