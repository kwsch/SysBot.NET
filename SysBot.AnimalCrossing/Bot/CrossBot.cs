using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    public sealed class CrossBot : SwitchRoutineExecutor<CrossBotConfig>
    {
        public readonly ConcurrentQueue<ItemRequest> Injections = new ConcurrentQueue<ItemRequest>();

        public CrossBot(CrossBotConfig cfg) : base(cfg) { }
        public override void SoftStop() => Config.AcceptingCommands = false;

        protected override async Task MainLoop(CancellationToken token)
        {
            int dropCount = 0;
            int idleCount = 0;
            while (!token.IsCancellationRequested)
            {
                if (!Config.AcceptingCommands)
                {
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                    continue;
                }

                if (Injections.TryDequeue(out var item))
                {
                    dropCount += await DropItems(item, token).ConfigureAwait(false);
                    idleCount = 0;
                }
                else if (dropCount != 0 && ++idleCount > 60)
                {
                    await CleanUp(token).ConfigureAwait(false);
                    dropCount = 0;
                    idleCount = 0;
                }
                else
                {
                    idleCount++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                }
            }
        }

        public async Task<int> DropItems(ItemRequest item, CancellationToken token)
        {
            int dropped = 0;
            foreach (var drop in item.Items)
            {
                LogUtil.LogInfo($"Dropping item for {item.User}", nameof(CrossBot));
                await DropItem(drop, token).ConfigureAwait(false);
                dropped++;
            }
            return dropped;
        }

        private async Task DropItem(byte[] drop, CancellationToken token)
        {
            // Exit out of any menus.
            for (int i = 0; i < 3; i++)
                await Click(SwitchButton.B, 0_500, token).ConfigureAwait(false);

            // Inject item.
            var poke = SwitchCommand.Poke(Config.Offset, drop);
            await Connection.SendAsync(poke, token);
            await Task.Delay(0_300, token).ConfigureAwait(false);

            // Open menu and use the last menu-option
            await Click(SwitchButton.X, 1_500, token).ConfigureAwait(false);
            await Click(SwitchButton.A, 0_500, token).ConfigureAwait(false);
            if (!Config.WrapAllItems)
                await Click(SwitchButton.DUP, 0_500, token).ConfigureAwait(false);
            await Click(SwitchButton.A, 1_000, token).ConfigureAwait(false);
            await Click(SwitchButton.X, 1_000, token).ConfigureAwait(false);

            // Exit out of any menus.
            for (int i = 0; i < 3; i++)
                await Click(SwitchButton.B, 0_500, token).ConfigureAwait(false);
        }

        private const int PickupCount = 5;

        private async Task CleanUp(CancellationToken token)
        {
            LogUtil.LogInfo("Picking up leftover items during idle time.", nameof(CrossBot));

            // Exit out of any menus.
            for (int i = 0; i < 3; i++)
                await Click(SwitchButton.B, 0_500, token).ConfigureAwait(false);

            // Pick up and delete.
            for (int i = 0; i < PickupCount; i++)
            {
                await Click(SwitchButton.Y, 2_000, token).ConfigureAwait(false);
                var poke = SwitchCommand.Poke(Config.Offset, Item.NONE.ToBytes());
                await Connection.SendAsync(poke, token);
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }
    }
}
