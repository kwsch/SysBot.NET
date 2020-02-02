using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor
    {
        public string? DumpFolder { get; set; }

        private const Daycare Location = Daycare.Route5;

        public EggBot(string ip, int port) : base(ip, port) { }

        protected override async Task MainLoop(CancellationToken token)
        {
            int enc = 0;
            var blank = PKMConverter.GetBlank(8).EncryptedPartyData;
            while (!token.IsCancellationRequested)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.
                while (true)
                {
                    await SetStick(LEFT, -19000, 19000, 500, CancellationToken.None).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, CancellationToken.None).ConfigureAwait(false);

                    await SetStick(LEFT, 19000, 19000, 500, CancellationToken.None).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, CancellationToken.None).ConfigureAwait(false);

                    await SetEggStepCounter(Location, token).ConfigureAwait(false);
                    await Task.Delay(250, token).ConfigureAwait(false);

                    bool checkEgg = await IsEggReady(Location, token).ConfigureAwait(false);
                    if (checkEgg)
                        break;
                }

                // Deletes Box 1 Slot 1 to have a consistent destination for the eggs.
                await Connection.WriteBytesAsync(blank, Box1Slot1, token).ConfigureAwait(false);

                await Task.Delay(1000, token).ConfigureAwait(false);

                for (int i = 0; i < 4; i++)
                    await Click(A, 500, token).ConfigureAwait(false);
                await Task.Delay(4000, token).ConfigureAwait(false);

                await Click(A, 250, CancellationToken.None).ConfigureAwait(false);
                await Task.Delay(1600, token).ConfigureAwait(false);
                await Click(A, 250, CancellationToken.None).ConfigureAwait(false);
                await Task.Delay(1600, token).ConfigureAwait(false);
                await Click(A, 250, CancellationToken.None).ConfigureAwait(false);

                var pk = await ReadBoxPokemon(1, 1, CancellationToken.None).ConfigureAwait(false);
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
                Console.WriteLine($"Encounter: {enc}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                enc++;
            }

            Console.WriteLine($"{Environment.NewLine}Done!");
            Console.ReadLine();
        }
    }
}
