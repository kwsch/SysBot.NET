using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

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
            Task.Run(InitializeCoreStrings);
            Task.Run(() => EncounterEvent.RefreshMGDB());
            InitializeTrainerDatabase(cfg);
            InitializeSettings(cfg);

            // Legalizer.AllowBruteForce = false;
        }

        private static void InitializeSettings(LegalitySettings cfg)
        {
            APILegality.SetAllLegalRibbons = cfg.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = cfg.SetMatchingBalls;
            APILegality.UseXOROSHIRO = cfg.UseXOROSHIRO;
            Legalizer.AllowBruteForce = cfg.AllowBruteForce;
        }

        private static void InitializeTrainerDatabase(LegalitySettings cfg)
        {
            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            string OT = cfg.GenerateOT;
            int TID = cfg.GenerateTID16;
            int SID = cfg.GenerateSID16;
            int lang = (int) cfg.GenerateLanguage;

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
                if (exist == fallback)
                    TrainerSettings.Register(fallback);
            }

            var trainer = TrainerSettings.GetSavedTrainerData(PKX.Generation);
            PKMConverter.SetPrimaryTrainer(trainer);
        }

        private static void InitializeCoreStrings()
        {
            var lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.Substring(0, 2);
            Util.SetLocalization(typeof(LegalityCheckStrings), lang);
            Util.SetLocalization(typeof(MessageStrings), lang);
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);

            // Update Legality Analysis strings
            LegalityAnalysis.MoveStrings = GameInfo.Strings.movelist;
            LegalityAnalysis.SpeciesStrings = GameInfo.Strings.specieslist;
        }

        public static bool CanBeTraded(this PKM pkm)
        {
            return pkm.Species switch
            {
                (int)Species.Kyurem when pkm.AltForm != 0 => false,
                (int)Species.Necrozma when pkm.AltForm != 0 => false,
                _ => true
            };
        }

        public static ITrainerInfo GetTrainerInfo(int gen) => TrainerSettings.GetSavedTrainerData(gen);

        public static PKM GetLegal(this ITrainerInfo sav, IBattleTemplate set, out string res)
        {
            var result =  sav.GetLegalFromSet(set, out var type);
            res = type.ToString();
            return result;
        }

        public static PKM LegalizePokemon(this PKM pk) => pk.Legalize();
        public static IBattleTemplate GetTemplate(ShowdownSet set) => new RegenTemplate(set);
    }
}
