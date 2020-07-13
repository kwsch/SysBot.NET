using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class YouTubeQueue
    {
        public PK8 Pokemon { get; set; }
        public PokeTradeTrainerInfo Trainer { get; set; }
        public string UserName { get; set; }
        public string DisplayName => Trainer.TrainerName;

        public YouTubeQueue(PK8 pkm, PokeTradeTrainerInfo trainer, string username)
        {
            Pokemon = pkm;
            Trainer = trainer;
            UserName = username;
        }
    }
}
