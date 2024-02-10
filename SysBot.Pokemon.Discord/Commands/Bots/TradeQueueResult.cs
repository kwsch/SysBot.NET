using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class TradeQueueResult
    {
        public bool Success { get; set; }

        public TradeQueueResult(bool success)
        {
            Success = success;
        }
    }
}
