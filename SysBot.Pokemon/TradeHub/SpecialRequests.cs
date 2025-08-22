using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.IO;

namespace SysBot.Pokemon.TradeHub
{
    /// <summary>
    /// Handles special modification requests for Pokemon during trades.
    /// Provides functionality for clone changes including shininess, IVs, nature, language, and other attributes.
    /// </summary>
    public static class SpecialRequests
    {
        private static readonly object _sync2 = new();
        private static readonly string ItemPath = @"0items.txt";
        private static readonly char[] separator = ['\n'];
        private static readonly Dictionary<string, int> UserListSpecialReqCount = [];

        #region Item ID Constants
        // Pokeballs
        private const int ITEM_MASTER_BALL = 1;
        private const int ITEM_ULTRA_BALL = 2;
        private const int ITEM_GREAT_BALL = 3;
        private const int ITEM_POKE_BALL = 4;

        // Shiny Items
        private const int ITEM_ANTIDOTE = 18;
        private const int ITEM_BURN_HEAL = 19;
        private const int ITEM_ICE_HEAL = 20;
        private const int ITEM_AWAKENING = 21;
        private const int ITEM_PARALYZE_HEAL = 22;

        // IV/Stat Items
        private const int ITEM_FULL_HEAL = 27;
        private const int ITEM_REVIVE = 28;
        private const int ITEM_FRESH_WATER = 30;
        private const int ITEM_SODA_POP = 31;
        private const int ITEM_LEMONADE = 32;
        private const int ITEM_POKE_DOLL = 63;

        // Language Items
        private const int ITEM_PP_UP = 51;
        private const int ITEM_GUARD_SPEC = 55;
        private const int ITEM_DIRE_HIT = 56;
        private const int ITEM_X_ATTACK = 57;
        private const int ITEM_X_DEFEND = 58;
        private const int ITEM_X_SPEED = 59;
        private const int ITEM_X_ACCURACY = 60;
        private const int ITEM_X_SP_ATK = 61;
        private const int ITEM_X_SP_DEF = 62;

        // Mint Items
        private const int MINT_START = 1231;
        private const int MINT_END = 1251;

        // Tera Shard Items
        private const int TERA_SHARD_START = 1862;
        private const int TERA_SHARD_END = 1879;
        private const int STELLAR_TERA_SHARD = 2549;

        // Perfect IVs
        private static readonly int[] PerfectIVs = [31, 31, 31, 31, 31, 31];
        private static readonly int[] PhysicalIVs = [31, 0, 31, 0, 31, 31];
        private static readonly int[] SpecialIVs = [31, 0, 31, 31, 31, 31];
        private static readonly int[] TrickRoomIVs = [31, 31, 31, 0, 31, 31];
        #endregion

        /// <summary>
        /// Represents the type of special trade modification being performed.
        /// </summary>
        public enum SpecialTradeType
        {
            None,
            ItemReq,
            BallReq,
            SanitizeReq,
            StatChange,
            TeraChange,
            Shinify,
            WonderCard,
            FailReturn
        }

        // needed for systemctl service on linux for mono to find
        private static Dictionary<string, int> SpecificItemRequests => CollectItemReqs();

        /// <summary>
        /// Processes special modification requests based on held items or nicknames.
        /// </summary>
        /// <typeparam name="T">The Pokemon type (PK8, PK9, etc.)</typeparam>
        /// <param name="pk">The Pokemon to process</param>
        /// <param name="caller">The routine executor handling this trade</param>
        /// <param name="detail">Trade details including user information</param>
        /// <param name="TrainerName">The trainer name for OT modifications</param>
        /// <param name="sav">Save file for loading wonder cards</param>
        /// <returns>The type of special trade performed</returns>
        public static SpecialTradeType CheckItemRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail, string TrainerName, SaveFile sav) where T : PKM, new()
        {
            int startingHeldItem = pk.HeldItem;

            // Check for Home Tracker
            var hasHomeTracker = pk is IHomeTrack ht && ht.Tracker != 0;

            // Log held item
            LogHeldItem(pk, startingHeldItem, caller);

            // Get reward item
            int heldItemNew = GetRewardItem(TrainerName);

            // Check nickname-based requests first (these don't need Home Tracker checks)
            if (pk.Nickname.StartsWith('!'))
                return HandleItemRequest(ref pk, caller, detail);

            if (pk.Nickname.StartsWith('?') || pk.Nickname.StartsWith('？'))
                return HandleBallRequest(ref pk, caller, detail);

            if (pk.Nickname.Contains("pls") && typeof(T) == typeof(PK8))
                return HandleWonderCardRequest(ref pk, caller, detail, sav);

            // Process held item-based requests
            return ProcessHeldItemRequest(ref pk, caller, detail, TrainerName, hasHomeTracker, heldItemNew, startingHeldItem);
        }

        /// <summary>
        /// Logs the held item information for debugging purposes.
        /// </summary>
        private static void LogHeldItem<T>(T pk, int heldItem, PokeRoutineExecutor<T> caller) where T : PKM, new()
        {
            GameStrings str = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
            var allitems = str.GetItemStrings(pk.Context, GameVersion.SWSH);

            if (heldItem > 0 && heldItem < allitems.Length)
            {
                var itemHeld = allitems[heldItem];
                caller.Log($"Item held: {itemHeld}");
            }
            else
            {
                caller.Log($"Held item was outside the bounds of the Array, or nothing was held: {heldItem}");
            }
        }

        /// <summary>
        /// Gets the reward item for a specific trainer from the configuration file.
        /// </summary>
        /// <param name="trainerName">The trainer name to look up</param>
        /// <returns>The item ID to give as reward, defaults to Master Ball</returns>
        private static int GetRewardItem(string trainerName)
        {
            var specs = SpecificItemRequests;
            return specs.TryGetValue(trainerName, out var itemId) ? itemId : ITEM_MASTER_BALL;
        }

        /// <summary>
        /// Processes held item-based modification requests.
        /// </summary>
        private static SpecialTradeType ProcessHeldItemRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            string trainerName, bool hasHomeTracker, int rewardItem, int heldItem) where T : PKM, new()
        {
            // Pokeball-based OT/Nickname changes
            if (heldItem >= ITEM_ULTRA_BALL && heldItem <= ITEM_POKE_BALL)
                return ProcessPokeballRequest(ref pk, caller, detail, trainerName, hasHomeTracker, rewardItem);

            // Shiny modifications
            if ((heldItem >= ITEM_ANTIDOTE && heldItem <= ITEM_PARALYZE_HEAL) || pk.IsEgg)
                return ProcessShinyRequest(ref pk, caller, detail, hasHomeTracker, rewardItem);

            // IV/Stat modifications
            if (IsStatChangeItem(heldItem))
                return ProcessStatChangeRequest(ref pk, caller, detail, hasHomeTracker, rewardItem, heldItem);

            // Tera Type modifications (PK9 only)
            if (pk is PK9 pk9 && IsTeraShardItem(heldItem))
                return ProcessTeraTypeRequest(ref pk, pk9, caller, detail, hasHomeTracker, rewardItem);

            // Language modifications
            if (heldItem >= ITEM_PP_UP && heldItem <= ITEM_X_SP_DEF && heldItem != 52 && heldItem != 53 && heldItem != 54)
                return ProcessLanguageRequest(ref pk, caller, detail, rewardItem, heldItem);

            // Nature modifications (Mints)
            if (heldItem >= MINT_START && heldItem <= MINT_END)
                return ProcessNatureRequest(ref pk, caller, detail, hasHomeTracker, rewardItem, heldItem);

            return SpecialTradeType.None;
        }

        /// <summary>
        /// Checks if an item ID corresponds to a stat change item.
        /// </summary>
        private static bool IsStatChangeItem(int item)
        {
            return item == ITEM_FULL_HEAL || item == ITEM_REVIVE ||
                   (item >= ITEM_FRESH_WATER && item <= ITEM_LEMONADE) ||
                   item == ITEM_POKE_DOLL;
        }

        /// <summary>
        /// Checks if an item ID corresponds to a Tera Shard.
        /// </summary>
        private static bool IsTeraShardItem(int item)
        {
            return (item >= TERA_SHARD_START && item <= TERA_SHARD_END) || item == STELLAR_TERA_SHARD;
        }

        /// <summary>
        /// Processes Pokeball-based OT and nickname modifications.
        /// </summary>
        private static SpecialTradeType ProcessPokeballRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            string trainerName, bool hasHomeTracker, int rewardItem) where T : PKM, new()
        {
            if (!HandleHomeTrackerForCloneChange(hasHomeTracker, caller, detail))
                return SpecialTradeType.FailReturn;

            switch (pk.HeldItem)
            {
                case ITEM_ULTRA_BALL:
                    pk.ClearNickname();
                    pk.OriginalTrainerName = trainerName;
                    break;
                case ITEM_GREAT_BALL:
                    pk.OriginalTrainerName = trainerName;
                    break;
                case ITEM_POKE_BALL:
                    pk.ClearNickname();
                    break;
            }

            ApplyCommonModifications(ref pk, rewardItem);
            LegalizeIfNotLegal(ref pk, caller, detail);
            return SpecialTradeType.SanitizeReq;
        }

        /// <summary>
        /// Processes shiny modification requests.
        /// </summary>
        private static SpecialTradeType ProcessShinyRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            bool hasHomeTracker, int rewardItem) where T : PKM, new()
        {
            if (!HandleHomeTrackerForCloneChange(hasHomeTracker, caller, detail))
                return SpecialTradeType.FailReturn;

            if (pk.HeldItem == ITEM_PARALYZE_HEAL)
            {
                pk.SetUnshiny();
            }
            else
            {
                var type = (pk.HeldItem == ITEM_BURN_HEAL || pk.HeldItem == ITEM_AWAKENING || pk.IsEgg)
                    ? Shiny.AlwaysSquare : Shiny.AlwaysStar;

                if (pk.HeldItem == ITEM_ICE_HEAL || pk.HeldItem == ITEM_AWAKENING)
                    pk.IVs = PerfectIVs;

                CommonEdits.SetShiny(pk, type);
            }

            LegalizeIfNotLegal(ref pk, caller, detail);

            if (!pk.IsEgg)
            {
                ApplyCommonModifications(ref pk, rewardItem);
            }

            return SpecialTradeType.Shinify;
        }

        /// <summary>
        /// Processes IV and stat modification requests.
        /// </summary>
        private static SpecialTradeType ProcessStatChangeRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            bool hasHomeTracker, int rewardItem, int heldItem) where T : PKM, new()
        {
            if (!HandleHomeTrackerForCloneChange(hasHomeTracker, caller, detail))
                return SpecialTradeType.FailReturn;

            // Apply IV/Level changes based on item
            switch (heldItem)
            {
                case ITEM_FULL_HEAL:
                    pk.IVs = PerfectIVs;
                    break;
                case ITEM_REVIVE:
                    pk.IVs = PhysicalIVs;
                    break;
                case ITEM_FRESH_WATER:
                    pk.IVs = SpecialIVs;
                    break;
                case ITEM_SODA_POP:
                    pk.CurrentLevel = 100;
                    break;
                case ITEM_LEMONADE:
                    pk.IVs = PerfectIVs;
                    pk.CurrentLevel = 100;
                    break;
                case ITEM_POKE_DOLL:
                    pk.IVs = TrickRoomIVs;
                    break;
            }

            // Clear hyper training from IV switched mons
            if (pk is IHyperTrain iht)
                iht.HyperTrainClear();

            ApplyCommonModifications(ref pk, rewardItem);
            LegalizeIfNotLegal(ref pk, caller, detail);
            return SpecialTradeType.StatChange;
        }

        /// <summary>
        /// Processes Tera Type modification requests for Generation 9 Pokemon.
        /// </summary>
        private static SpecialTradeType ProcessTeraTypeRequest<T>(ref T _, PK9 pk9, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            bool hasHomeTracker, int rewardItem) where T : PKM, new()
        {
            if (!HandleHomeTrackerForCloneChange(hasHomeTracker, caller, detail))
                return SpecialTradeType.FailReturn;

            // Map item to Tera Type
            MoveType teraType = pk9.HeldItem switch
            {
                1862 => MoveType.Normal,
                1863 => MoveType.Fire,
                1864 => MoveType.Water,
                1865 => MoveType.Electric,
                1866 => MoveType.Grass,
                1867 => MoveType.Ice,
                1868 => MoveType.Fighting,
                1869 => MoveType.Poison,
                1870 => MoveType.Ground,
                1871 => MoveType.Flying,
                1872 => MoveType.Psychic,
                1873 => MoveType.Bug,
                1874 => MoveType.Rock,
                1875 => MoveType.Ghost,
                1876 => MoveType.Dragon,
                1877 => MoveType.Dark,
                1878 => MoveType.Steel,
                1879 => MoveType.Fairy,
                STELLAR_TERA_SHARD => (MoveType)TeraTypeUtil.Stellar,
                _ => MoveType.Normal
            };

            pk9.TeraTypeOverride = teraType;
            var pk9AsT = (T)(PKM)pk9;
            LegalizeIfNotLegal(ref pk9AsT, caller, detail);

            SimpleEdits.SetRecordFlags(pk9, []);
            pk9.HeldItem = rewardItem;

            return SpecialTradeType.TeraChange;
        }

        /// <summary>
        /// Processes language modification requests.
        /// </summary>
        private static SpecialTradeType ProcessLanguageRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            int rewardItem, int heldItem) where T : PKM, new()
        {
            // Map item to language
            var language = heldItem switch
            {
                ITEM_PP_UP => LanguageID.Italian,
                ITEM_GUARD_SPEC => LanguageID.Japanese,
                ITEM_DIRE_HIT => LanguageID.English,
                ITEM_X_ATTACK => LanguageID.German,
                ITEM_X_DEFEND => LanguageID.French,
                ITEM_X_SPEED => LanguageID.Spanish,
                ITEM_X_ACCURACY => LanguageID.Korean,
                ITEM_X_SP_ATK => LanguageID.ChineseT,
                ITEM_X_SP_DEF => LanguageID.ChineseS,
                _ => LanguageID.English
            };

            pk.Language = (int)language;
            pk.ClearNickname();

            LegalizeIfNotLegal(ref pk, caller, detail);
            ApplyCommonModifications(ref pk, rewardItem);

            return SpecialTradeType.SanitizeReq;
        }

        /// <summary>
        /// Processes nature modification requests using mints.
        /// </summary>
        private static SpecialTradeType ProcessNatureRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            bool hasHomeTracker, int rewardItem, int heldItem) where T : PKM, new()
        {
            if (!HandleHomeTrackerForCloneChange(hasHomeTracker, caller, detail))
                return SpecialTradeType.FailReturn;

            GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
            var items = strings.GetItemStrings(pk.Context, GameVersion.SWSH);
            var itemName = items[heldItem];
            var natureName = itemName.Split(' ')[0];

            if (!Enum.TryParse(natureName, out Nature result))
            {
                detail.SendNotification(caller, "Nature request was not found in the db.");
                return SpecialTradeType.FailReturn;
            }

            pk.Nature = pk.StatNature = result;
            ApplyCommonModifications(ref pk, rewardItem);
            LegalizeIfNotLegal(ref pk, caller, detail);

            return SpecialTradeType.StatChange;
        }

        /// <summary>
        /// Handles item requests using nickname prefix '!'.
        /// </summary>
        private static SpecialTradeType HandleItemRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail) where T : PKM, new()
        {
            var itemLookup = pk.Nickname[1..].Replace(" ", string.Empty);
            GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
            var items = strings.GetItemStrings(pk.Context, GameVersion.SWSH);
            int item = Array.FindIndex(items, z => z.Replace(" ", string.Empty).StartsWith(itemLookup, StringComparison.OrdinalIgnoreCase));

            if (item < 0)
            {
                detail.SendNotification(caller, "Item request was invalid. Check spelling & gen.");
                return SpecialTradeType.None;
            }

            pk.HeldItem = item;
            LegalizeIfNotLegal(ref pk, caller, detail);

            return SpecialTradeType.ItemReq;
        }

        /// <summary>
        /// Handles ball requests using nickname prefix '?' or '？'.
        /// </summary>
        private static SpecialTradeType HandleBallRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail) where T : PKM, new()
        {
            var ballLookup = pk.Nickname[1..].Replace(" ", string.Empty).Replace("poke", "poké").ToLower();
            GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
            var balls = strings.balllist;

            int ball = Array.FindIndex(balls, z => z.Replace(" ", string.Empty).StartsWith(ballLookup, StringComparison.OrdinalIgnoreCase));
            if (ball < 0)
            {
                detail.SendNotification(caller, "Ball request was invalid. Check spelling & gen.");
                return SpecialTradeType.None;
            }

            pk.Ball = (byte)ball;
            LegalizeIfNotLegal(ref pk, caller, detail);

            return SpecialTradeType.BallReq;
        }

        /// <summary>
        /// Handles wonder card requests using nickname containing 'pls'.
        /// </summary>
        private static SpecialTradeType HandleWonderCardRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail,
            SaveFile sav) where T : PKM, new()
        {
            T? loaded = LoadEvent<T>(pk.Nickname.Replace("pls", string.Empty).ToLower(), sav);

            if (loaded == null)
            {
                detail.SendNotification(caller, "This isn't a valid request!");
                return SpecialTradeType.FailReturn;
            }

            pk = loaded;
            return SpecialTradeType.WonderCard;
        }

        /// <summary>
        /// Applies common modifications to Pokemon after special requests.
        /// </summary>
        private static void ApplyCommonModifications<T>(ref T pk, int rewardItem) where T : PKM
        {
            pk.SetRecordFlags([]);
            pk.HeldItem = rewardItem;
            if (!pk.IsEgg)
                pk.ClearNickname();
        }

        /// <summary>
        /// Collects item requests from the configuration file.
        /// </summary>
        private static Dictionary<string, int> CollectItemReqs()
        {
            Dictionary<string, int> tmp = [];
            try
            {
                lock (_sync2)
                {
                    if (!File.Exists(ItemPath))
                        return tmp;

                    var rawText = File.ReadAllText(ItemPath);
                    foreach (var st in rawText.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var reqs = st.Split(',');
                        if (reqs.Length >= 2 && int.TryParse(reqs[1], out int itemId))
                            tmp[reqs[0]] = itemId;
                    }
                }
            }
            catch { }
            return tmp;
        }

        /// <summary>
        /// Attempts to legalize a Pokemon if it's not already legal.
        /// </summary>
        private static void LegalizeIfNotLegal<T>(ref T pkm, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail) where T : PKM, new()
        {
            var tempPk = pkm.Clone();
            var la = new LegalityAnalysis(pkm);

            if (!la.Valid)
            {
                detail.SendNotification(caller, "This request isn't legal! Attempting to legalize...");
                caller.Log(la.Report());

                try
                {
                    pkm = (T)pkm.LegalizePokemon();
                }
                catch (Exception e)
                {
                    caller.Log($"Legalization failed: {e.Message}");
                    return;
                }
            }
            else
            {
                return;
            }

            // Restore original trainer name if needed
            pkm.OriginalTrainerName = tempPk.OriginalTrainerName;

            // Re-check legality
            la = new LegalityAnalysis(pkm);
            if (!la.Valid)
            {
                pkm = (T)pkm.LegalizePokemon();
            }
        }

        /// <summary>
        /// Loads a wonder card event from file.
        /// </summary>
        private static T? LoadEvent<T>(string eventName, SaveFile sav) where T : PKM, new()
        {
            // Try loading different wondercard formats
            string[] extensions = [".wc9", ".wc8", ".wc7", ".wc6", ".pgf"];

            foreach (var ext in extensions)
            {
                string pathwc = Path.Combine("wc", eventName + ext);
                if (!File.Exists(pathwc))
                    continue;

                byte[] wcData = File.ReadAllBytes(pathwc);
                MysteryGift? wc = ext switch
                {
                    ".wc9" => new WC9(wcData),
                    ".wc8" => new WC8(wcData),
                    ".wc7" => new WC7(wcData),
                    ".wc6" => new WC6(wcData),
                    ".pgf" => new PGF(wcData),
                    _ => null
                };

                if (wc == null)
                    continue;

                var pkLoaded = wc.ConvertToPKM(sav);

                // Convert to appropriate format if needed
                if (!pkLoaded.SWSH)
                {
                    pkLoaded = EntityConverter.ConvertToType(pkLoaded, typeof(T), out _);
                    if (pkLoaded != null)
                    {
                        pkLoaded.CurrentHandler = 1;
                        QuickLegalize(ref pkLoaded);
                    }
                }

                if (pkLoaded != null)
                    return (T)pkLoaded;
            }

            return null;
        }

        /// <summary>
        /// Quick legalization for loaded events.
        /// </summary>
        private static void QuickLegalize(ref PKM pkm)
        {
            var la = new LegalityAnalysis(pkm);
            if (!la.Valid)
                pkm = pkm.LegalizePokemon();
        }

        /// <summary>
        /// Validates if clone changes can be applied to Pokemon with Home Tracker.
        /// </summary>
        /// <param name="hasHomeTracker">Whether the Pokemon has a Home Tracker</param>
        /// <param name="caller">The routine executor</param>
        /// <param name="detail">Trade details</param>
        /// <returns>True if clone changes can proceed, false if blocked</returns>
        private static bool HandleHomeTrackerForCloneChange<T>(bool hasHomeTracker, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail) where T : PKM, new()
        {
            // If the Pokemon doesn't have a Home Tracker, we can proceed with clone changes
            if (!hasHomeTracker)
                return true;

            // Pokemon has a Home Tracker - block all clone changes for safety
            detail.SendNotification(caller, "Cannot apply clone changes to this Pokemon. It has a Home Tracker, and modifying it would invalidate the tracker. Please use a Pokemon without a Home Tracker.");
            caller.Log("Clone change blocked - Pokemon has Home Tracker. Modifications would invalidate the tracker.");
            return false;
        }
    }
}
