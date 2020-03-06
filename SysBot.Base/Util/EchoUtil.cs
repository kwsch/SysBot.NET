using System;
using System.Collections.Generic;

namespace SysBot.Base
{
    public static class EchoUtil
    {
        public static readonly List<Action<string>> Forwarders = new List<Action<string>>();

        public static void Echo(string message)
        {
            foreach (var fwd in Forwarders)
                fwd(message);
            LogUtil.LogInfo(message, "Echo");
        }
    }
}