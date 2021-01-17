using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Base
{
    public class BotRunner<T> where T : class, IConsoleBotConfig
    {
        public readonly List<BotSource<T>> Bots = new();

        public bool IsRunning => Bots.Any(z => z.IsRunning);
        public bool RunOnce { get; private set; }

        public virtual void Add(RoutineExecutor<T> bot)
        {
            if (Bots.Any(z => z.Bot.Connection.Equals(bot.Connection)))
                throw new ArgumentException($"{nameof(bot.Connection)} has already been added.");
            Bots.Add(new BotSource<T>(bot));
        }

        public virtual bool Remove(IConsoleBotConfig cfg, bool callStop)
        {
            var match = GetBot(cfg);
            if (match == null)
                return false;

            if (callStop)
                match.Stop();
            return Bots.Remove(match);
        }

        public virtual void InitializeStart()
        {
            RunOnce = true;
        }

        public virtual void StartAll()
        {
            foreach (var b in Bots)
                b.Start();
        }

        public virtual void StopAll()
        {
            foreach (var b in Bots)
                b.Stop();
        }

        public virtual void PauseAll()
        {
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Pause();
        }

        public virtual void ResumeAll()
        {
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Resume();
        }

        public BotSource<T>? GetBot(IConsoleBotConfig config) => Bots.Find(z => z.Bot.Config.Equals(config));
        public BotSource<T>? GetBot(string ip) => Bots.Find(z => z.Bot.Config.Matches(ip));
    }
}
