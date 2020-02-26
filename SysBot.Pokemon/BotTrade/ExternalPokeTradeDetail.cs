using System.IO;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public sealed class ExternalPokeTradeDetail<TPoke> : PokeTradeDetail<TPoke> where TPoke : PKM, new()
    {
        public readonly string SourcePath;
        public readonly string DestinationPath;

        public ExternalPokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, TradeFile paths)
            : base(pkm, info, notifier, type, code)
        {
            SourcePath = paths.SourcePath;
            DestinationPath = paths.DestinationPath;
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
            LogUtil.LogInfo("Moved processed trade to destination folder.", completedBy.Connection.Name);
        }

        public override void TradeFinished(PokeRoutineExecutor routine, TPoke result)
        {
            base.TradeFinished(routine, result);
            RelocateProcessedFile(routine);
        }
    }

    public sealed class TradeFile
    {
        public readonly string SourcePath;
        public readonly string DestinationPath;

        public TradeFile(string source, string dest)
        {
            SourcePath = source;
            DestinationPath = dest;
        }
    }
}