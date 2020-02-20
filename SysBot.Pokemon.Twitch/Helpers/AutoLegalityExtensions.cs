using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using  PKHeX.Core.AutoMod;

namespace SysBot.Pokemon.Twitch
{
    public static class AutoLegalityExtensions
    {
        private static bool Initialized;

        static AutoLegalityExtensions() => EnsureInitialized();

        public static void EnsureInitialized()
        {
            if (Initialized)
                return;
            Initialized = true;
            InitializeAutoLegality();
        }

        private static void InitializeAutoLegality()
        {
            Task.Run(InitializeCoreStrings);
            Task.Run(() => EncounterEvent.RefreshMGDB());
            InitializeTrainerDatabase();
            InitializeSettings();

            // Legalizer.AllowBruteForce = false;
        }

        private static void InitializeSettings()
        {
            var cfg = TwitchBot.Info.Hub.Config;
            APILegality.SetAllLegalRibbons = cfg.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = cfg.SetMatchingBalls;
        }

        private static void InitializeTrainerDatabase()
        {
            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            var cfg = TwitchBot.Info.Hub.Config;
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

            for (int i = 1; i < PKX.Generation; i++)
            {
                var fallback = GetFallbackBlank(i);
                var exist = TrainerSettings.GetSavedTrainerData(i, fallback);
                if (exist == fallback)
                    TrainerSettings.Register(fallback);
            }

            var trainer = TrainerSettings.GetSavedTrainerData(7);
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
    }
}
