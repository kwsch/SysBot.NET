using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class LedyRequest<T> where T : PKM, new()
    {
        public readonly string Nickname;
        public readonly T RequestInfo;

        public LedyRequest(T requestInfo, string nickname)
        {
            RequestInfo = requestInfo;
            Nickname = nickname;
        }
    }
}