using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    public sealed class CrossBot : SwitchRoutineExecutor<CrossBotConfig>
    {
        public readonly ConcurrentQueue<ItemRequest> Injections = new ConcurrentQueue<ItemRequest>();
        public bool CleanRequested { private get; set; }
        public string DodoCode { get; set; } = "No code set yet.";

        public CrossBot(CrossBotConfig cfg) : base(cfg) { }
        public override void SoftStop() => Config.AcceptingCommands = false;

        protected override async Task MainLoop(CancellationToken token)
        {
            // Disconnect our virtual controller; will reconnect once we send a button command after a request.
            await Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None);
            LogUtil.LogInfo("Connected to bot. Starting main loop!", Config.IP);

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
                else if ((Config.AutoClean && dropCount != 0 && ++idleCount > 60) || CleanRequested)
                {
                    await CleanUp(token).ConfigureAwait(false);
                    dropCount = 0;
                    idleCount = 0;
                    CleanRequested = false;
                }
                else
                {
                    idleCount++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                }
            }
        }

        public async Task<int> DropItems(ItemRequest drop, CancellationToken token)
        {
            int dropped = 0;
            bool first = true;
            foreach (var item in drop.Items)
            {
                await DropItem(item, first, token).ConfigureAwait(false);
                first = false;
                dropped++;
            }
            return dropped;
        }

        private async Task DropItem(Item item, bool first, CancellationToken token)
        {
            // Exit out of any menus.
            if (first)
            {
                for (int i = 0; i < 3; i++)
                    await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);
            }

            LogUtil.LogInfo($"Injecting Item: {item.DisplayItemId:X4}.", Config.IP);

            // Inject item.
            var data = item.ToBytesClass();
            var poke = SwitchCommand.Poke(Config.Offset, data);
            await Connection.SendAsync(poke, token);
            await Task.Delay(0_300, token).ConfigureAwait(false);

            // Open player inventory and open the currently selected item slot -- assumed to be the config offset.
            await Click(SwitchButton.X, 0_900, token).ConfigureAwait(false);
            await Click(SwitchButton.A, 0_400, token).ConfigureAwait(false);

            // Navigate down to the "drop item" option.
            var downCount = item.GetItemDropOption();
            for (int i = 0; i < downCount; i++)
                await Click(SwitchButton.DDOWN, 0_400, token).ConfigureAwait(false);

            // Drop item, close menu.
            await Click(SwitchButton.A, 0_400, token).ConfigureAwait(false);
            await Click(SwitchButton.X, 0_400, token).ConfigureAwait(false);

            // Exit out of any menus (fail-safe)
            for (int i = 0; i < 2; i++)
                await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);
        }

        private const int PickupCount = 5;

        private async Task CleanUp(CancellationToken token)
        {
            LogUtil.LogInfo("Picking up leftover items during idle time.", Config.IP);

            // Exit out of any menus.
            for (int i = 0; i < 3; i++)
                await Click(SwitchButton.B, 0_400, token).ConfigureAwait(false);

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
