using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor
    {
        public string? DumpFolder { get; set; }

        private const Daycare Location = Daycare.Route5;

        public EggBot(string ip, int port) : base(ip, port) { }
        public EggBot(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        protected override async Task MainLoop(CancellationToken token)
        {
            int encounterCount = 0;
            var blank = new PK8();
            while (!token.IsCancellationRequested)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.
                while (true)
                {
                    await SetEggStepCounter(Location, token).ConfigureAwait(false);

                    // Walk Diagonally Left
                    await SetStick(LEFT, -19000, 19000, 500, token).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                    // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                    await SetStick(LEFT, 19000, 19000, 550, token).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                    bool checkEgg = await IsEggReady(Location, token).ConfigureAwait(false);
                    if (checkEgg)
                        break;
                }

                // Deletes Box 1 Slot 1 to have a consistent destination for the eggs.
                await SetBoxPokemon(blank, 0, 0, token).ConfigureAwait(false);

                await Task.Delay(1000, token).ConfigureAwait(false);

                for (int i = 0; i < 4; i++)
                    await Click(A, 500, token).ConfigureAwait(false);
                await Task.Delay(4000, token).ConfigureAwait(false);

                await Click(A, 250, token).ConfigureAwait(false);
                await Task.Delay(1600, token).ConfigureAwait(false);
                await Click(A, 250, token).ConfigureAwait(false);
                await Task.Delay(1600, token).ConfigureAwait(false);
                await Click(A, 250, token).ConfigureAwait(false);

                var pk = await ReadBoxPokemon(0, 0, token).ConfigureAwait(false);
                await Task.Delay(200, token).ConfigureAwait(false);
                if (pk.Species == 0)
                    continue;

                await ReadDumpB1S1(DumpFolder, token).ConfigureAwait(false);

                if (pk.IsShiny)
                {
                    Console.WriteLine("Shiny Found!");
                    break;
                }

                await Task.Delay(200, token).ConfigureAwait(false);
                Console.WriteLine($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                encounterCount++;
            }

            Console.WriteLine($"{Environment.NewLine}Done!");
            Console.ReadLine();
        }
    }
}
