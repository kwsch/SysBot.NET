using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class TradeQueueResult
    {
        public bool Success { get; set; }
        public List<Pictocodes> LGCode { get; set; }

        public TradeQueueResult(bool success, List<Pictocodes> lgcode = null)
        {
            Success = success;
            LGCode = lgcode;
        }
    }
}
