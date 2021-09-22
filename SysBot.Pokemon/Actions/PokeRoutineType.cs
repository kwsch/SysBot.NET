namespace SysBot.Pokemon
{
    public enum PokeRoutineType
    {
        Idle = 0,

        SurpriseTrade = 1,

        FlexTrade = 2,
        LinkTrade = 3,
        SeedCheck = 4,
        Clone = 5,
        Dump = 6,

        EggFetch = 7,
        FossilBot = 8,
        RaidBot = 9,
        EncounterBot = 10,

        RemoteControl = 100,

        // Add your own custom bots here so they don't clash for future main-branch bot releases.
    }

    public static class PokeRoutineTypeExtensions
    {
        public static bool IsTradeBot(this PokeRoutineType type) => type is >=PokeRoutineType.FlexTrade and <=PokeRoutineType.Dump;
    }
}