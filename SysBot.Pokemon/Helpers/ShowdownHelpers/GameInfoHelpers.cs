using PKHeX.Core;
using System;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class GameInfoHelpers<T> where T : PKM, new()
    {
        public static IPersonalAbility12 GetPersonalInfo(ushort speciesIndex)
        {
            if (typeof(T) == typeof(PK8))
                return PersonalTable.SWSH.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PB8))
                return PersonalTable.BDSP.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PA8))
                return PersonalTable.LA.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PK9))
                return PersonalTable.SV.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PB7))
                return PersonalTable.GG.GetFormEntry(speciesIndex, 0);

            throw new ArgumentException("Type does not have a recognized personal table.", typeof(T).Name);
        }

        public static IPersonalFormInfo GetPersonalFormInfo(ushort speciesIndex)
        {
            if (typeof(T) == typeof(PK8))
                return PersonalTable.SWSH.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PB8))
                return PersonalTable.BDSP.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PA8))
                return PersonalTable.LA.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PK9))
                return PersonalTable.SV.GetFormEntry(speciesIndex, 0);
            if (typeof(T) == typeof(PB7))
                return PersonalTable.GG.GetFormEntry(speciesIndex, 0);

            throw new ArgumentException("Type does not have a recognized personal form table.", typeof(T).Name);
        }

        public static EntityContext GetGeneration()
        {
            if (typeof(T) == typeof(PK8))
                return EntityContext.Gen8;
            if (typeof(T) == typeof(PB8))
                return EntityContext.Gen8b;
            if (typeof(T) == typeof(PA8))
                return EntityContext.Gen8a;
            if (typeof(T) == typeof(PK9))
                return EntityContext.Gen9;
            if (typeof(T) == typeof(PB7))
                return EntityContext.Gen7b;

            throw new ArgumentException("Type does not have a recognized generation.", typeof(T).Name);
        }

        private static string GetLanguageCode(GameVersion version)
        {
            int languageIndex = GetLanguageIndex(version);
            return GetLanguageCodeFromIndex(languageIndex);
        }

        private static int GetLanguageIndex(GameVersion version)
        {
            return 2;
        }

        private static string GetLanguageCodeFromIndex(int index)
        {
            return index switch
            {
                1 => "ja",
                2 => "en",
                3 => "fr",
                4 => "it",
                5 => "de",
                6 => "es",
                8 => "ko",
                9 => "zh-Hans",
                10 => "zh-Hant",
                _ => "en", // Default to English
            };
        }

        public static ILearnSource GetLearnSource(PKM pk)
        {
            if (pk is PK9)
                return LearnSource9SV.Instance;
            if (pk is PB8)
                return LearnSource8BDSP.Instance;
            if (pk is PA8)
                return LearnSource8LA.Instance;
            if (pk is PK8)
                return LearnSource8SWSH.Instance;
            if (pk is PB7)
                return LearnSource7GG.Instance;
            throw new ArgumentException("Unsupported PKM type.", nameof(pk));
        }
    }
}
