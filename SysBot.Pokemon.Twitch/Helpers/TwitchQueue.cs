using PKHeX.Core;

namespace SysBot.Pokemon.Twitch;

public sealed record TwitchQueue<T>(T Entity, PokeTradeTrainerInfo Trainer, string Username, bool IsSubscriber)
    where T : PKM, new()
{
    public string DisplayName => Trainer.TrainerName;
}
