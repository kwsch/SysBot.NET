using PKHeX.Core;

namespace SysBot.Pokemon;

public interface ISeedSearchHandler<T> where T : PKM, new()
{
    void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot);
}

public class NoSeedSearchHandler<T> : ISeedSearchHandler<T> where T : PKM, new()
{
    public void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot)
    {
        const string msg = "Seed searching implementation not found. " +
                           "Please let the person hosting the bot know that they need to provide the required Z3 files.";
        detail.SendNotification(bot, msg);
    }
}
