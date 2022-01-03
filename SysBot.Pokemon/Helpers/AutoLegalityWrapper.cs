﻿using System;
using PKHeX.Core;
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

            for (int i = 1; i < PKX.Generation + 1; i++)
            {
                var versions = GameUtil.GetVersionsInGeneration(i, PKX.Generation);
                foreach (var v in versions)
                {
                    var fallback = new SimpleTrainerInfo(v)
                    {
                        Language = lang,
                        TID = TID,
                        SID = SID,
                        OT = OT,
                    };
                    var exist = TrainerSettings.GetSavedTrainerData(v, i, fallback);
                    if (exist is SimpleTrainerInfo) // not anything from files; this assumes ALM returns SimpleTrainerInfo for non-user-provided fake templates.
                        TrainerSettings.Register(fallback);
                }
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
            if (pkm.IsNicknamed && StringsUtil.IsSpammyString(pkm.Nickname))
                return false;
            if (StringsUtil.IsSpammyString(pkm.OT_Name) && !IsFixedOT(new LegalityAnalysis(pkm).EncounterOriginal, pkm))
                return false;
            return !FormInfo.IsFusedForm(pkm.Species, pkm.Form, pkm.Format);
        }

        public static bool IsFixedOT(IEncounterTemplate t, PKM pkm) => t switch
        {
            EncounterTrade tr => tr.HasTrainerName,
            MysteryGift g => !g.EggEncounter && g switch
            {
                WC8 wc8 => wc8.GetHasOT(pkm.Language),
                WB8 wb8 => wb8.GetHasOT(pkm.Language),
                { Generation: >= 5 } gift => gift.OT_Name.Length > 0,
                _ => true,
            },
            _ => false,
        };

        public static ITrainerInfo GetTrainerInfo<T>() where T : PKM, new()
        {
            if (typeof(T) == typeof(PK8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.SWSH, 8);
            if (typeof(T) == typeof(PB8))
                return TrainerSettings.GetSavedTrainerData(GameVersion.BDSP, 8);

            throw new ArgumentException("Type does not have a recognized trainer fetch.", typeof(T).Name);
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
