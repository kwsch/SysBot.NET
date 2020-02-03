using System.IO;
using NLog;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> where TPoke : PKM
    {
        public readonly int Code;
        public readonly TPoke TradeData;
        public readonly PokeTradeTrainerInfo Trainer;
        public readonly IPokeTradeNotifier<TPoke> Notifier;

        private const int RandomCode = -1;
        public bool IsRandomCode => Code == RandomCode;

        public string? SourcePath { get; set; }
        public string? DestinationPath { get; set; }

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, int code = RandomCode)
        {
            Code = code;
            TradeData = pkm;
            Trainer = info;
            Notifier = notifier;
        }

        public void InitializeTrade(PokeRoutineExecutor routine) => Notifier.TradeInitialize(routine, this);
        public void SearchTrade(PokeRoutineExecutor routine) => Notifier.TradeSearching(routine, this);

        public void CompleteTrade(PokeRoutineExecutor routine)
        {
            Notifier.TradeFinished(routine, this);
            RelocateProcessedFile(routine);
        }

        private void RelocateProcessedFile(PokeRoutineExecutor completedBy)
        {
            if (SourcePath == null || !Directory.Exists(Path.GetDirectoryName(SourcePath)) || !File.Exists(SourcePath))
                return;
            if (DestinationPath == null || !Directory.Exists(Path.GetDirectoryName(DestinationPath)))
                return;

            if (File.Exists(DestinationPath))
                File.Delete(DestinationPath);
            File.Move(SourcePath, DestinationPath);
            LogUtil.Log(LogLevel.Info, "Moved processed trade to destination folder.", completedBy.Connection.Name);
        }
    }
}