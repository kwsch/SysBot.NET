using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchQueue
    {
        public PK8 Pokemon { get; }
        public PokeTradeTrainerInfo Trainer { get; }
        public string UserName { get; }
        public string DisplayName => Trainer.TrainerName;
        public bool IsSubscriber { get; }

        public TwitchQueue(PK8 pkm, PokeTradeTrainerInfo trainer, string username, bool subscriber)
        {
            Pokemon = pkm;
            Trainer = trainer;
            UserName = username;
            IsSubscriber = subscriber;
        }
    }
}
