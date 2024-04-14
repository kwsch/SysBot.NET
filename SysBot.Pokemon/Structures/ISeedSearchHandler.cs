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
        const string msg = "Anwendung zur Seed-Suche nicht gefunden. " +
                           "Bitte teilen Sie der Person, die den Bot hostet mit, dass sie die erforderlichen Z3-Dateien bereitstellen muss.";
        detail.SendNotification(bot, msg);
    }
}
