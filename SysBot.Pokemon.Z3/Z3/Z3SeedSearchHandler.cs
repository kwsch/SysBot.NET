using PKHeX.Core;

namespace SysBot.Pokemon.Z3
{
    public class Z3SeedSearchHandler<T> : ISeedSearchHandler<T> where T : PKM, new()
    {
        public void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot)
        {
            var ec = pkm.EncryptionConstant;
            var pid = pkm.PID;
            int[] IVs = pkm.IVs;
            PKX.ReorderSpeedLast(IVs);

            if (settings.ShowAllZ3Results)
            {
                var matches = Z3Search.GetAllSeeds(ec, pid, IVs, settings.ResultDisplayMode);
                foreach (var match in matches)
                {
                    var lump = new PokeTradeSummary("Calculated Seed:", match);
                    detail.SendNotification(bot, lump);
                }
            }
            else
            {
                var match = Z3Search.GetFirstSeed(ec, pid, IVs, settings.ResultDisplayMode);
                var lump = new PokeTradeSummary("Calculated Seed:", match);
                detail.SendNotification(bot, lump);
            }
        }
    }
}