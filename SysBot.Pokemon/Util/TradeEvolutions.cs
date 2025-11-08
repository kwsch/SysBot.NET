using PKHeX.Core;
using static PKHeX.Core.Species;

namespace SysBot.Pokemon;

public static class TradeEvolutions
{
    const int everstone = 229;
    const int kingsrock = 221;
    const int metalcoat = 233;
    const int dragonscale = 235;
    const int upgrade = 252;
    const int dubiousdisc = 324;
    const int protector = 321;
    const int electirizer = 322;
    const int magmarizer = 323;
    const int reapercloth = 325;
    const int deepseatooth = 226;
    const int deepseascale = 227;
    const int prismscale = 537;
    const int sachet = 647;
    const int whippeddream = 646;

    public static bool WillTradeEvolve(ushort species, byte form, int helditem = 0, ushort request = 0) => (Species)species switch
    {
        Kadabra => true,
        Machoke => helditem != everstone,
        Graveler => helditem != everstone,
        Haunter => helditem != everstone,
        Boldore => helditem != everstone,
        Gurdurr => helditem != everstone,
        Phantump => helditem != everstone,
        Pumpkaboo => helditem != everstone,

        Poliwhirl => helditem == kingsrock,
        Slowpoke => form == 0 && helditem == kingsrock,
        Onix => helditem == metalcoat,
        Scyther => helditem == metalcoat,
        Seadra => helditem == dragonscale,
        Porygon => helditem == upgrade,
        Porygon2 => helditem == dubiousdisc,
        Rhydon => helditem == protector,
        Electabuzz => helditem == electirizer,
        Magmar => helditem == magmarizer,
        Dusclops => helditem == reapercloth,
        Clamperl => helditem == deepseatooth || helditem == deepseascale,
        Feebas => helditem == prismscale,
        Spritzee => helditem == sachet,
        Swirlix => helditem == whippeddream,

        Shelmet => request == (ushort)Karrablast,
        Karrablast => request == (ushort)Shelmet,

        _ => false,
    };
}
