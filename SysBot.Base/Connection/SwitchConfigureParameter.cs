namespace SysBot.Base
{
    public enum SwitchConfigureParameter
    {
        /// <summary>
        /// Amount of time (milliseconds) the sysmodule sleeps between loops.
        /// </summary>
        mainLoopSleepTime,

        /// <summary>
        /// Amount of time (milliseconds) the sysmodule holds a button before releasing when <see cref="SwitchCommand.Click"/> is requested.
        /// </summary>
        buttonClickSleepTime,
    }
}