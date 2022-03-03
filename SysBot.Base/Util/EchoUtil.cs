using System;
using System.Collections.Generic;

namespace SysBot.Base
{
    public static class EchoUtil
    {
        public static readonly List<Action<string>> Forwarders = new();

        public static void Echo(string message)
        {
            foreach (var fwd in Forwarders)
            {
                try
                {
                    fwd(message);
                }
                catch (Exception ex)
                {
                    LogUtil.LogInfo($"Exception: {ex} occurred while trying to echo: {message} to the forwarder: {fwd}", "Echo");
                    LogUtil.LogSafe(ex, "Echo");
                }
            }
            LogUtil.LogInfo(message, "Echo");
        }
    }
}