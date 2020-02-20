using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class LedyResponse<T> where T : PKM, new()
    {
        public T Receive { get; }
        public LedyResponseType Type { get; }

        public LedyResponse(T pk, LedyResponseType type)
        {
            Receive = pk;
            Type = type;
        }
    }
}