using System;
using System.IO;
using NLog;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> : IEquatable<PokeTradeDetail<TPoke>> where TPoke : PKM
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

        public void TradeInitialize(PokeRoutineExecutor routine) => Notifier.TradeInitialize(routine, this);
        public void TradeSearching(PokeRoutineExecutor routine) => Notifier.TradeSearching(routine, this);
        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);

        public void TradeFinished(PokeRoutineExecutor routine, TPoke result)
        {
            Notifier.TradeFinished(routine, this, result);
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

        public bool Equals(PokeTradeDetail<TPoke>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ReferenceEquals(Trainer, other.Trainer);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PokeTradeDetail<TPoke>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Trainer.GetHashCode();
                hashCode = (hashCode * 397) ^ (SourcePath != null ? SourcePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DestinationPath != null ? DestinationPath.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}