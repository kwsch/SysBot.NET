﻿using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System.IO;
using System.Threading;

namespace SysBot.Pokemon
{
    public static class AutoLegalityWrapper
    {
        private static bool Initialized;

        public static void EnsureInitialized(LegalitySettings cfg)
        {
            if (Initialized)
                return;
            Initialized = true;
            InitializeAutoLegality(cfg);
        }

        private static void InitializeAutoLegality(LegalitySettings cfg)
        {
            InitializeCoreStrings();
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB(cfg.MGDBPath);
            InitializeTrainerDatabase(cfg);
            InitializeSettings(cfg);
        }

        private static void InitializeSettings(LegalitySettings cfg)
        {
            APILegality.SetAllLegalRibbons = cfg.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = cfg.SetMatchingBalls;
            APILegality.ForceSpecifiedBall = cfg.ForceSpecifiedBall;
            APILegality.UseXOROSHIRO = cfg.UseXOROSHIRO;
            Legalizer.EnableEasterEggs = cfg.EnableEasterEggs;
            APILegality.AllowTrainerOverride = cfg.AllowTrainerDataOverride;
            APILegality.AllowBatchCommands = cfg.AllowBatchCommands;
            APILegality.Timeout = cfg.Timeout;
        }

        private static void InitializeTrainerDatabase(LegalitySettings cfg)
        {
            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            string OT = cfg.GenerateOT;
            int TID = cfg.GenerateTID16;
            int SID = cfg.GenerateSID16;
            int lang = (int)cfg.GenerateLanguage;

            var externalSource = cfg.GeneratePathTrainerInfo;
            if (!string.IsNullOrWhiteSpace(externalSource) && Directory.Exists(externalSource))
                TrainerSettings.LoadTrainerDatabaseFromPath(externalSource);

            SaveFile GetFallbackBlank(int generation)
            {
                var blankSav = SaveUtil.GetBlankSAV(generation, OT);
                blankSav.Language = lang;
                blankSav.TID = TID;
                blankSav.SID = SID;
                blankSav.OT = OT;
                return blankSav;
            }

            for (int i = 1; i < PKX.Generation + 1; i++)
            {
                var fallback = GetFallbackBlank(i);
                var exist = TrainerSettings.GetSavedTrainerData(i, fallback);
                if (ReferenceEquals(exist, fallback))
                    TrainerSettings.Register(fallback);
            }

            var trainer = TrainerSettings.GetSavedTrainerData(PKX.Generation);
            PKMConverter.SetPrimaryTrainer(trainer);
        }

        private static void InitializeCoreStrings()
        {
            var lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName[..2];
            LocalizationUtil.SetLocalization(typeof(LegalityCheckStrings), lang);
            LocalizationUtil.SetLocalization(typeof(MessageStrings), lang);
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            ParseSettings.ChangeLocalizationStrings(GameInfo.Strings.movelist, GameInfo.Strings.specieslist);
        }

        public static bool CanBeTraded(this PKM pkm)
        {
            return !FormInfo.IsFusedForm(pkm.Species, pkm.Form, pkm.Format);
        }

        public static ITrainerInfo GetTrainerInfo(int gen) => TrainerSettings.GetSavedTrainerData(gen);

        public static PKM GetLegal(this ITrainerInfo sav, IBattleTemplate set, out string res)
        {
            var result = sav.GetLegalFromSet(set, out var type);
            res = type.ToString();
            return result;
        }

        public static PKM LegalizePokemon(this PKM pk) => pk.Legalize();
        public static IBattleTemplate GetTemplate(ShowdownSet set) => new RegenTemplate(set);
    }
}
