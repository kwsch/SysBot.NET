using PKHeX.Core;

namespace SysBot.Pokemon;

public class LedyResponse<T>(T Receive, LedyResponseType Type)
    where T : PKM, new()
{
    public T Receive { get; } = Receive;
    public LedyResponseType Type { get; } = Type;
}
