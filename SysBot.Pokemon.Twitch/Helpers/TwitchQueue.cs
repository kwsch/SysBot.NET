using PKHeX.Core;

namespace SysBot.Pokemon.Twitch;

public class TwitchQueue<T>(T Entity, PokeTradeTrainerInfo Trainer, string Username, bool Subscriber)
    where T : PKM, new()
{
    public T Entity { get; } = Entity;
    public PokeTradeTrainerInfo Trainer { get; } = Trainer;
    public string UserName { get; } = Username;
    public string DisplayName => Trainer.TrainerName;
    public bool IsSubscriber { get; } = Subscriber;
}
