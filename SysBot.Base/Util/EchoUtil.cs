using System;
using System.Collections.Generic;

namespace SysBot.Base;

public static class EchoUtil
{
    public static readonly List<Action<string>> Forwarders = [];

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
                LogUtil.LogInfo($"Ausnahme: {ex} beim Versuch, ein Echo zu erzeugen, aufgetreten: {message} an den Versender: {fwd}", "Echo");
                LogUtil.LogSafe(ex, "Echo");
            }
        }
        LogUtil.LogInfo(message, "Echo");
    }
}
