using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class SlotQualityCheck
    {
        public readonly PKM? Data;
        public readonly SlotQuality Quality;

        public SlotQualityCheck(PKM? pkm, SlotQuality quality)
        {
            Data = pkm;
            Quality = quality;
        }

        public SlotQualityCheck(PKM? pkm)
        {
            Data = pkm;
            Quality = GetQuality(pkm);
        }

        private static SlotQuality GetQuality(PKM? pkm)
        {
            if (pkm == null)
                return SlotQuality.BadData;
            if (!(pkm.Species == 0 || pkm.IsEgg))
                return pkm.ChecksumValid ? SlotQuality.HasData : SlotQuality.BadData;
            return SlotQuality.Overwritable;
        }
    }
}