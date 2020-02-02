using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor
    {
        public readonly PokeTradeHub<PK8> Hub;
        public string? DumpFolder { get; set; }

        public EggBot(PokeTradeHub<PK8> hub, string ip, int port) : base(ip, port) => Hub = hub;

        protected override async Task MainLoop(CancellationToken token)
        {
            
            int enc = 0;
            while (!token.IsCancellationRequested)
            {
                await SetEggStepCounter(Daycare.Route5);
                Thread.Sleep(1000);
                bool EggIsReady = false;
                while (!EggIsReady)
                {
                    await SetStick(LEFT, -19000, 19000,500,CancellationToken.None);

                    await SetStick(LEFT, 0, 0, 500, CancellationToken.None);

                    await SetStick(LEFT, 19000, 19000, 500, CancellationToken.None);

                    await SetStick(LEFT, 0, 0, 500, CancellationToken.None);

                    await SetEggStepCounter(Daycare.Route5);
                    Thread.Sleep(250);

                    if (await IsEggReady(Daycare.Route5)){ EggIsReady = !EggIsReady; }
                }

                await Connection.WriteBytesAsync(PKMConverter.GetBlank(8).EncryptedPartyData,Box1Slot1,CancellationToken.None); // < -- Deletes Slot 1 too keep track of the Eggs.

                Thread.Sleep(1000);

                for (int i = 0; i < 4; i++)
                {
                    await Click(A, 500, CancellationToken.None);
                }

                Thread.Sleep(4000);
                await Click(A, 250, CancellationToken.None);
                Thread.Sleep(1600);
                await Click(A, 250, CancellationToken.None);
                Thread.Sleep(1600);
                await Click(A, 250, CancellationToken.None);

                PKM pk = await ReadBoxPokemon(1, 1, CancellationToken.None);
                Thread.Sleep(200);
                if (pk.Species != 0)
                {
                    //Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Eggs\");
                    //ile.WriteAllBytes(Directory.GetCurrentDirectory() + @"\Eggs\" + pk.FileName, pk.EncryptedPartyData);
                    if (pk.IsShiny)
                    {
                        Console.WriteLine("Shiny Found!");
                        break;
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                    Console.WriteLine("Encounter: " + enc + ":\n\n" + ShowdownSet.GetShowdownText(pk) + "\n\n");
                    enc++;
                }

            }
            Console.WriteLine("\ndone!");
            Console.ReadLine();
        }
        }
    }
