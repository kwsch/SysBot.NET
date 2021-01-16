using System;

namespace SysBot.Base
{
    internal sealed class FakeBotConnectionConfig : IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>
    {
        public bool IsValid() => false;
        public bool Matches(string magic) => false;
        public ISwitchConnectionSync CreateSync() => throw new InvalidOperationException();
        public ISwitchConnectionAsync CreateAsynchronous() => throw new InvalidOperationException();
    }
}
