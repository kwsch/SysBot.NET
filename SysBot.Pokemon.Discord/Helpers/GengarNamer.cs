using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

    public sealed class GengarNamer : IFileNamer<PKM>
    {
        public string Name => "Default";

        public string GetName(PKM obj)
        {
            if (obj is GBPKM gb)
                return GetGBPKM(gb);
            return GetRegular(obj);
        }

        private static string GetConditionalTeraType(PKM pk)
        {
            if (pk is not ITeraType t)
                return string.Empty;
            var type = t.GetTeraType();
            var type_str = ((byte)type == TeraTypeUtil.Stellar) ? "Stellar" : type.ToString();
            return $"Tera({type_str})";
        }

        private static string GetRegular(PKM pk)
        {
            string form = pk.Form > 0 ? $"-{pk.Form:00}" : string.Empty;
            string shinytype = GetShinyTypeString(pk);

            string IVList = $"{pk.IV_HP}.{pk.IV_ATK}.{pk.IV_DEF}.{pk.IV_SPA}.{pk.IV_SPD}.{pk.IV_SPE}";

            int metYear = pk.MetYear;
            string metYearString = metYear > 0 ? $"{metYear + 2000}" : string.Empty;

            string speciesName = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);
            if (pk is IGigantamax { CanGigantamax: true })
                speciesName += "-Gmax";

            return $"{speciesName}{shinytype}-{GetConditionalTeraType(pk)}-{GetNature(pk)}-{GetAbility(pk)}-{IVList}-{metYearString}-{GetVersion(pk)}";
        }

        private static string GetVersion(PKM pk)
        {
            if (pk.E) return "Emerald";
            if (pk.FRLG) return "FRLG";
            if (pk.Pt) return "Pt";
            if (pk.HGSS) return "HGSS";
            if (pk.BW) return "BW";
            if (pk.B2W2) return "B2W2";
            if (pk.XY) return "XY";
            if (pk.AO) return "ORAS";
            if (pk.SM) return "SM";
            if (pk.USUM) return "USUM";
            if (pk.GO) return "GO";
            if (pk.VC1) return "VC1";
            if (pk.VC2) return "VC2";
            if (pk.LGPE) return "LGPE";
            if (pk.SWSH) return "SWSH";
            if (pk.BDSP) return "BBDSP";
            if (pk.LA) return "PLA";
            if (pk.SV) return "SV";
            return "Unknown";
        }

        private static string GetNature(PKM pk)
        {
            var nature = pk.Nature;
            var strings = Util.GetNaturesList("en");
            if ((uint)nature >= strings.Length)
                nature = 0;
            return strings[(uint)nature];
        }


        private static string GetAbility(PKM pk)
        {
            int abilityIndex = pk.Ability;
            // You need to implement a method similar to Util.GetNaturesList for abilities
            var abilityStrings = Util.GetAbilitiesList("en");
            if ((uint)abilityIndex >= abilityStrings.Length)
                abilityIndex = 0;
            return abilityStrings[abilityIndex];
        }

        private static string GetShinyTypeString(PKM pk)
        {
            if (!pk.IsShiny)
                return string.Empty;
            if (pk.Format >= 8 && (pk.ShinyXor == 0 || pk.FatefulEncounter || pk.Version == GameVersion.GO))
                return " ■";
            return " ★";
        }

        private static string GetGBPKM(GBPKM gb)
        {
            string form = gb.Form > 0 ? $"-{gb.Form:00}" : string.Empty;
            string star = gb.IsShiny ? " ★" : string.Empty;
            int metYear = gb.MetYear;
            string metYearString = metYear > 0 ? $"-{metYear + 2000}" : string.Empty;
            string IVList = $"{gb.IV_HP}.{gb.IV_ATK}.{gb.IV_DEF}.{gb.IV_SPA}.{gb.IV_SPD}.{gb.IV_SPE}";
            string speciesName = SpeciesName.GetSpeciesNameGeneration(gb.Species, (int)LanguageID.English, gb.Format);
            return $"{speciesName} - {gb.Species:000}{form}{star} - {IVList} - {metYearString}";
        }
    }

