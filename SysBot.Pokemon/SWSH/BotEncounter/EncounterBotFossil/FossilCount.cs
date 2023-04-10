using PKHeX.Core;
using System;
using static SysBot.Pokemon.FossilSpecies;

namespace SysBot.Pokemon
{
    public class FossilCount
    {
        private int Bird;
        private int Fish;
        private int Drake;
        private int Dino;

        // Top Half: Select Down for fish if species type == Dracovish || Arctovish
        // Bottom Half: Select Down for dino if species type == Arctozolt || Arctovish
        public bool UseSecondOption1(FossilSpecies f) => Bird != 0 && f is Arctovish or Dracovish;
        public bool UseSecondOption2(FossilSpecies f) => Drake != 0 && f is Arctozolt or Arctovish;

        private void SetCount(int item, int count)
        {
            if (item == 1105)
                Bird = count;
            if (item == 1106)
                Fish = count;
            if (item == 1107)
                Drake = count;
            if (item == 1108)
                Dino = count;
        }

        private static readonly ushort[] Pouch_Treasure_SWSH =
        {
            086, 087, 088, 089, 090, 091, 092, 094, 106,
            571, 580, 581, 582, 583,
            795, 796,
            1105, 1106, 1107, 1108,
        };

        public static FossilCount GetFossilCounts(byte[] itemsBlock)
        {
            var pouch = GetTreasurePouch(itemsBlock);
            return ReadCounts(pouch);
        }

        private static FossilCount ReadCounts(InventoryPouch pouch)
        {
            var counts = new FossilCount();
            foreach (var item in pouch.Items)
                counts.SetCount(item.Index, item.Count);
            return counts;
        }

        private static InventoryPouch8 GetTreasurePouch(byte[] itemsBlock)
        {
            var pouch = new InventoryPouch8(InventoryType.MailItems, Pouch_Treasure_SWSH, 999, 0, 20);
            pouch.GetPouch(itemsBlock);
            return pouch;
        }

        public int PossibleRevives(FossilSpecies f) => f switch
        {
            Dracozolt => Math.Min(Bird, Drake),
            Arctozolt => Math.Min(Bird, Dino),
            Dracovish => Math.Min(Fish, Drake),
            Arctovish => Math.Min(Fish, Dino),
            _ => throw new ArgumentOutOfRangeException("Fossil species was invalid.", nameof(FossilSpecies)),
        };
    }
}
