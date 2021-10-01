namespace SysBot.Pokemon
{
    public enum EncounterMode
    {
        /// <summary>
        /// Bot will move back and forth in a straight vertical path to encounter Pokémon
        /// </summary>
        VerticalLine,

        /// <summary>
        /// Bot will move back and forth in a straight horizontal path to encounter Pokémon
        /// </summary>
        HorizontalLine,

        /// <summary>
        /// Bot will soft reset Eternatus
        /// </summary>
        Eternatus,

        /// <summary>
        /// Bot claims a gift and checks Box 1 Slot 1
        /// </summary>
        Gift,

        /// <summary>
        /// Bot checks a wild encounter and then resets the game
        /// </summary>
        Reset,

        /// <summary>
        /// Bot resets Regigigas
        /// </summary>
        Regigigas,

        /// <summary>
        /// Bot resets Motostoke Gym encounters
        /// </summary>
        MotostokeGym,
    }
}