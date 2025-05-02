using System;
using System.Collections.Generic;

namespace SysBot.Base;
public static class EchoUtil
{
    public static readonly List<Action<string>> Forwarders = [];
    public static readonly List<Action<string>> AbuseForwarders = [];

    public static void Echo(string message)
    {
        foreach (var fwd in Forwarders)
        {
            fwd(message);
        }
    }

    public static void EchoAbuseMessage(string message)
    {
        foreach (var fwd in AbuseForwarders)
        {
            fwd(message);
        }
    }
}
