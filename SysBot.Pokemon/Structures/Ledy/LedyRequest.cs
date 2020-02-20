using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class LedyRequest<T> where T : PKM, new()
    {
        public string Nickname;
        public IReceivable<T> RequestInfo;

        public LedyRequest(IReceivable<T> requestInfo, string nickname)
        {
            RequestInfo = requestInfo;
            Nickname = nickname;
        }
    }
}