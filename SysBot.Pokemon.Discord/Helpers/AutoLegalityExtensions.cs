using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon.Discord
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
            var cfg = SysCordInstance.Self.Hub.Config;
            APILegality.SetAllLegalRibbons = cfg.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = cfg.SetMatchingBalls;
        }

        private static void InitializeTrainerDatabase()
        {
            // Seed the Trainer Database with enough fake save files so that we return a generation sensitive format when needed.
            var cfg = SysCordInstance.Self.Hub.Config;
            string OT = cfg.GenerateOT;
            int TID = cfg.GenerateTID16;
            int SID = cfg.GenerateSID16;
            int lang = cfg.GenerateLanguage;

            var externalSource = cfg.GeneratePathSaveFiles;
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

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, ITrainerInfo sav, ShowdownSet set)
        {
            if (set.Species <= 0)
            {
                await channel.SendMessageAsync("Oops! I wasn't able to interpret your message! If you intended to convert something, please double check what you're pasting!").ConfigureAwait(false);
                return;
            }
            var pkm = sav.GetLegalFromSet(set, out var result);
            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[set.Species];
            var msg = la.Valid
                ? $"Here's your ({result}) legalized PKM for {spec}!"
                : $"Oops! I wasn't able to create something from that. Here's my best attempt for that {spec}!";
            await channel.SendPKMAsync(pkm, msg + $"\n{ReusableActions.GetFormattedShowdownText(pkm)}").ConfigureAwait(false);
        }

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content, int gen)
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = TrainerSettings.GetSavedTrainerData(gen);
            await channel.ReplyWithLegalizedSetAsync(sav, set).ConfigureAwait(false);
        }

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content)
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = TrainerSettings.GetSavedTrainerData(set.Format);
            await channel.ReplyWithLegalizedSetAsync(sav, set).ConfigureAwait(false);
        }

        public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, IAttachment att)
        {
            var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
            if (!download.Success)
            {
                await channel.SendMessageAsync(download.ErrorMessage).ConfigureAwait(false);
                return;
            }

            var pkm = download.Data!;
            if (new LegalityAnalysis(pkm).Valid)
            {
                await channel.SendMessageAsync($"{download.SanitizedFileName}: Already legal.").ConfigureAwait(false);
                return;
            }

            var legal = pkm.Legalize();
            if (legal == null || !new LegalityAnalysis(legal).Valid)
            {
                await channel.SendMessageAsync($"{download.SanitizedFileName}: Unable to legalize.").ConfigureAwait(false);
                return;
            }

            legal.RefreshChecksum();

            var msg = $"Here's your legalized PKM for {download.SanitizedFileName}!\n{ReusableActions.GetFormattedShowdownText(legal)}";
            await channel.SendPKMAsync(legal, msg).ConfigureAwait(false);
        }
    }
}