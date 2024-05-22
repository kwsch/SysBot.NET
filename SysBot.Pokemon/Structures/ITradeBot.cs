using System;

namespace SysBot.Pokemon;

public interface ITradeBot
{
    event EventHandler<Exception> ConnectionError;
    event EventHandler ConnectionSuccess;
}


