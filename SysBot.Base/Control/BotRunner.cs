using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Base
{
    public class BotRunner<T> where T : SwitchBotConfig
    {
        public readonly List<BotSource<T>> Bots = new List<BotSource<T>>();

        public bool IsRunning { get; private set; }
        public bool CanStop => IsRunning;
        public bool RunOnce { get; private set; }

        public virtual void Add(SwitchRoutineExecutor<T> bot)
        {
            if (Bots.Any(z => z.Bot.Connection.IP == bot.Connection.IP))
                throw new ArgumentException($"{nameof(bot.Connection.IP)} has already been added.");
            Bots.Add(new BotSource<T>(bot));
        }

        public virtual bool Remove(string ip, bool callStop)
        {
            var match = Bots.Find(z => z.Bot.Connection.IP == ip);
            if (match == null)
                return false;

            if (callStop)
                match.Stop();
            return Bots.Remove(match);
        }

        public virtual void StartAll()
        {
            foreach (var b in Bots)
                b.Start();
            RunOnce = IsRunning = true;
        }

        public virtual void StopAll()
        {
            foreach (var b in Bots)
                b.Stop();
            IsRunning = false;
        }

        public virtual void PauseAll()
        {
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Pause();
        }

        public virtual void ResumeAll()
        {
            IsRunning = true;
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Resume();
        }

        public BotSource<T>? GetBot(T config) => Bots.Find(z => z.Bot.Config == config);
    }
}