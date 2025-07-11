using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static SysBot.Pokemon.TradeSettings;

namespace SysBot.Pokemon.Helpers
{
    public abstract class AbstractTrade<T> where T : PKM, new()
    {
        public static readonly string[] MarkTitle =
        [
            " The Peckish",
            " The Sleepy",
            " The Dozy",
            " The Early Riser",
            " The Cloud Watcher",
            " The Sodden",
            " The Thunderstruck",
            " The Snow Frolicker",
            " The Shivering",
            " The Parched",
            " The Sandswept",
            " The Mist Drifter",
            " The Chosen One",
            " The Catch of The Day",
            " The Curry Connoisseur",
            " The Sociable",
            " The Recluse",
            " The Rowdy",
            " The Spacey",
            " The Anxious",
            " The Giddy",
            " The Radiant",
            " The Serene",
            " The Feisty",
            " The Daydreamer",
            " The Joyful",
            " The Furious",
            " The Beaming",
            " The Teary-Eyed",
            " The Chipper",
            " The Grumpy",
            " The Scholar",
            " The Rampaging",
            " The Opportunist",
            " The Stern",
            " The Kindhearted",
            " The Easily Flustered",
            " The Driven",
            " The Apathetic",
            " The Arrogant",
            " The Reluctant",
            " The Humble",
            " The Pompous",
            " The Lively",
            " The Worn-Out",
            " Of The Distant Past",
            " The Twinkling Star",
            " The Paldea Champion",
            " The Great",
            " The Teeny",
            " The Treasure Hunter",
            " The Reliable Partner",
            " The Gourmet",
            " The One-in-a-Million",
            " The Former Alpha",
            " The Unrivaled",
            " The Former Titan",
        ];

        public static readonly ushort[] ShinyLock = [  (ushort)Species.Victini, (ushort)Species.Keldeo, (ushort)Species.Volcanion, (ushort)Species.Cosmog, (ushort)Species.Cosmoem, (ushort)Species.Magearna, (ushort)Species.Marshadow, (ushort)Species.Eternatus,
                                                    (ushort)Species.Kubfu, (ushort)Species.Urshifu, (ushort)Species.Zarude, (ushort)Species.Glastrier, (ushort)Species.Spectrier, (ushort)Species.Calyrex ];

        public static T CherishHandler(MysteryGift mg, ITrainerInfo info)
        {
            var result = EntityConverterResult.None;
            var mgPkm = mg.ConvertToPKM(info);
            bool canConvert = EntityConverter.IsConvertibleToFormat(mgPkm, info.Generation);
            mgPkm = canConvert ? EntityConverter.ConvertToType(mgPkm, typeof(T), out result) : mgPkm;

            if (mgPkm is not null && result is EntityConverterResult.Success)
            {
                var enc = new LegalityAnalysis(mgPkm).EncounterMatch;
                mgPkm.SetHandlerandMemory(info, enc);

                if (mgPkm.TID16 is 0 && mgPkm.SID16 is 0)
                {
                    mgPkm.TID16 = info.TID16;
                    mgPkm.SID16 = info.SID16;
                }

                mgPkm.CurrentLevel = mg.LevelMin;
                if (mgPkm.Species is (ushort)Species.Giratina && mgPkm.Form > 0)
                    mgPkm.HeldItem = 112;
                else if (mgPkm.Species is (ushort)Species.Silvally && mgPkm.Form > 0)
                    mgPkm.HeldItem = mgPkm.Form + 903;
                else mgPkm.HeldItem = 0;
            }
            else
            {
                return new();
            }

            mgPkm = TrashBytes((T)mgPkm);
            var la = new LegalityAnalysis(mgPkm);
            if (!la.Valid)
            {
                mgPkm.SetRandomIVs(6);
                var text = ShowdownParsing.GetShowdownText(mgPkm);
                var set = new ShowdownSet(text);
                var template = AutoLegalityWrapper.GetTemplate(set);
                var pk = AutoLegalityWrapper.GetLegal(info, template, out _);
                pk.SetAllTrainerData(info);
                return (T)pk;
            }
            else
            {
                return (T)mgPkm;
            }
        }

        public static void DittoTrade(PKM pkm)
        {
            var dittoStats = new string[] { "atk", "spe", "spa" };
            var nickname = pkm.Nickname.ToLower();
            pkm.StatNature = pkm.Nature;
            pkm.MetLocation = pkm switch
            {
                PB8 => 400,
                PK9 => 28,
                _ => 162, // PK8
            };

            pkm.MetLevel = pkm switch
            {
                PB8 => 29,
                PK9 => 34,
                _ => pkm.MetLevel,
            };

            if (pkm is PK9 pk9)
            {
                pk9.ObedienceLevel = pk9.MetLevel;
                pk9.TeraTypeOriginal = PKHeX.Core.MoveType.Normal;
                pk9.TeraTypeOverride = (PKHeX.Core.MoveType)19;
            }

            pkm.Ball = 21;
            pkm.IVs = [31, nickname.Contains(dittoStats[0]) ? 0 : 31, 31, nickname.Contains(dittoStats[1]) ? 0 : 31, nickname.Contains(dittoStats[2]) ? 0 : 31, 31];
            TrashBytes(pkm, new LegalityAnalysis(pkm));
        }

        // https://github.com/Koi-3088/ForkBot.NET/blob/KoiTest/SysBot.Pokemon/Helpers/TradeExtensions.cs
        public static void EggTrade(PKM pk, IBattleTemplate template, bool nicknameEgg = true)
        {
            if (nicknameEgg)
            {
                pk.IsNicknamed = true;
                pk.Nickname = pk.Language switch
                {
                    1 => "タマゴ",
                    3 => "Œuf",
                    4 => "Uovo",
                    5 => "Ei",
                    7 => "Huevo",
                    8 => "알",
                    9 or 10 => "蛋",
                    _ => "Egg",
                };
            }
            else
            {
                pk.IsNicknamed = false;
                pk.Nickname = "";
            }

            pk.IsEgg = true;
            pk.EggLocation = pk switch
            {
                PB8 => 60010,
                PK9 => 30023,
                _ => 60002, //PK8
            };

            pk.MetDate = DateOnly.FromDateTime(DateTime.Now);
            pk.EggMetDate = pk.MetDate;
            pk.HeldItem = 0;
            pk.CurrentLevel = 1;
            pk.EXP = 0;
            pk.MetLevel = 1;
            pk.MetLocation = pk switch
            {
                PB8 => 65535,
                PK9 => 0,
                _ => 30002, //PK8
            };

            pk.CurrentHandler = 0;
            pk.OriginalTrainerFriendship = 1;
            pk.HandlingTrainerName = "";
            ClearHandlingTrainerTrash(pk);
            pk.HandlingTrainerFriendship = 0;
            pk.ClearMemories();
            pk.StatNature = pk.Nature;
            pk.SetEVs([0, 0, 0, 0, 0, 0]);

            MarkingApplicator.SetMarkings(pk);
            RibbonApplicator.RemoveAllValidRibbons(pk);

            pk.ClearRelearnMoves();

            if (pk is PK8 pk8)
            {
                pk8.HandlingTrainerLanguage = 0;
                pk8.HandlingTrainerGender = 0;
                pk8.HandlingTrainerMemory = 0;
                pk8.HandlingTrainerMemoryFeeling = 0;
                pk8.HandlingTrainerMemoryIntensity = 0;
                pk8.DynamaxLevel = pk8.GetSuggestedDynamaxLevel(pk8, 0);
            }
            else if (pk is PB8 pb8)
            {
                pb8.HandlingTrainerLanguage = 0;
                pb8.HandlingTrainerGender = 0;
                pb8.HandlingTrainerMemory = 0;
                pb8.HandlingTrainerMemoryFeeling = 0;
                pb8.HandlingTrainerMemoryIntensity = 0;
                pb8.DynamaxLevel = pb8.GetSuggestedDynamaxLevel(pb8, 0);
                ClearNicknameTrash(pk);
            }
            else if (pk is PK9 pk9)
            {
                pk9.HandlingTrainerLanguage = 0;
                pk9.HandlingTrainerGender = 0;
                pk9.HandlingTrainerMemory = 0;
                pk9.HandlingTrainerMemoryFeeling = 0;
                pk9.HandlingTrainerMemoryIntensity = 0;
                pk9.ObedienceLevel = 1;
                pk9.Version = 0;
                pk9.BattleVersion = 0;
                pk9.TeraTypeOverride = (PKHeX.Core.MoveType)19;
            }

            var la = new LegalityAnalysis(pk);
            var enc = la.EncounterMatch;
            pk.MaximizeFriendship();

            Span<ushort> relearn = stackalloc ushort[4];
            la.GetSuggestedRelearnMoves(relearn, enc);
            pk.SetRelearnMoves(relearn);
            if (pk is ITechRecord t)
            {
                t.ClearRecordFlags();
            }
            pk.SetSuggestedMoves();

            pk.Move1_PPUps = pk.Move2_PPUps = pk.Move3_PPUps = pk.Move4_PPUps = 0;
            pk.SetMaximumPPCurrent(pk.Moves);
            pk.SetSuggestedHyperTrainingData();
        }

        public static string FormOutput(ushort species, byte form, out string[] formString)
        {
            var strings = GameInfo.GetStrings("en");
            formString = FormConverter.GetFormList(species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, typeof(T) == typeof(PK8) ? EntityContext.Gen8 : EntityContext.Gen4);
            if (formString.Length is 0)
                return string.Empty;

            formString[0] = "";
            if (form >= formString.Length)
                form = (byte)(formString.Length - 1);

            return formString[form].Contains('-') ? formString[form] : formString[form] == "" ? "" : $"-{formString[form]}";
        }

        private static void ClearNicknameTrash(PKM pokemon)
        {
            switch (pokemon)
            {
                case PK9 pk9:
                    ClearTrash(pk9.NicknameTrash, pk9.Nickname);
                    break;
                case PA8 pa8:
                    ClearTrash(pa8.NicknameTrash, pa8.Nickname);
                    break;
                case PB8 pb8:
                    ClearTrash(pb8.NicknameTrash, pb8.Nickname);
                    break;
                case PB7 pb7:
                    ClearTrash(pb7.NicknameTrash, pb7.Nickname);
                    break;
                case PK8 pk8:
                    ClearTrash(pk8.NicknameTrash, pk8.Nickname);
                    break;
            }
        }
        private static void ClearTrash(Span<byte> trash, string name)
        {
            trash.Clear();
            int maxLength = trash.Length / 2;
            int actualLength = Math.Min(name.Length, maxLength);
            for (int i = 0; i < actualLength; i++)
            {
                char value = name[i];
                trash[i * 2] = (byte)value;
                trash[(i * 2) + 1] = (byte)(value >> 8);
            }
            if (actualLength < maxLength)
            {
                trash[actualLength * 2] = 0x00;
                trash[(actualLength * 2) + 1] = 0x00;
            }
        }
        private static void ClearHandlingTrainerTrash(PKM pk)
        {
            switch (pk)
            {
                case PK8 pk8:
                    ClearTrash(pk8.HandlingTrainerTrash, "");
                    break;
                case PB8 pb8:
                    ClearTrash(pb8.HandlingTrainerTrash, "");
                    break;
                case PK9 pk9:
                    ClearTrash(pk9.HandlingTrainerTrash, "");
                    break;
            }
        }

        public static bool HasAdName(T pk, out string ad)
        {
            const string domainPattern = @"(?<=\w)\.(com|org|net|gg|xyz|io|tv|co|me|us|uk|ca|de|fr|jp|au|eu|ch|it|nl|ru|br|in|fun|info|blog|int|gov|edu|app|art|biz|bot|buzz|dev|eco|fan|fans|forum|free|game|help|host|inc|icu|live|lol|market|media|news|ninja|now|one|ong|online|page|porn|pro|red|sale|sex|sexy|shop|site|store|stream|tech|tel|top|tube|uno|vip|website|wiki|work|world|wtf|xxx|zero|youtube|zone|nyc|onion|bit|crypto|meme)\b";
            bool ot = Regex.IsMatch(pk.OriginalTrainerName, domainPattern, RegexOptions.IgnoreCase);
            bool nick = Regex.IsMatch(pk.Nickname, domainPattern, RegexOptions.IgnoreCase);
            ad = ot ? pk.OriginalTrainerName : nick ? pk.Nickname : "";
            return ot || nick;
        }

        public static bool HasMark(IRibbonIndex pk, out RibbonIndex result, out string markTitle)
        {
            result = default;
            markTitle = string.Empty;

            if (pk is IRibbonSetMark9 ribbonSetMark)
            {
                if (ribbonSetMark.RibbonMarkMightiest)
                {
                    result = RibbonIndex.MarkMightiest;
                    markTitle = " The Unrivaled";
                    return true;
                }
                else if (ribbonSetMark.RibbonMarkAlpha)
                {
                    result = RibbonIndex.MarkAlpha;
                    markTitle = " The Former Alpha";
                    return true;
                }
                else if (ribbonSetMark.RibbonMarkTitan)
                {
                    result = RibbonIndex.MarkTitan;
                    markTitle = " The Former Titan";
                    return true;
                }
                else if (ribbonSetMark.RibbonMarkJumbo)
                {
                    result = RibbonIndex.MarkJumbo;
                    markTitle = " The Great";
                    return true;
                }
                else if (ribbonSetMark.RibbonMarkMini)
                {
                    result = RibbonIndex.MarkMini;
                    markTitle = " The Teeny";
                    return true;
                }
            }

            for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
            {
                if (pk.GetRibbon((int)mark))
                {
                    result = mark;
                    markTitle = MarkTitle[(int)mark - (int)RibbonIndex.MarkLunchtime];
                    return true;
                }
            }

            return false;
        }

        public static string PokeImg(PKM pkm, bool canGmax, bool fullSize, ImageSize? preferredImageSize = null)
        {
            bool md = false;
            bool fd = false;
            string[] baseLink;

            if (fullSize)
            {
                baseLink = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/512x512/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            }
            else if (preferredImageSize.HasValue)
            {
                baseLink = preferredImageSize.Value switch
                {
                    ImageSize.Size256x256 => "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/256x256/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_'),
                    ImageSize.Size128x128 => "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_'),
                    _ => "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/256x256/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_'),
                };
            }
            else
            {
                baseLink = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/256x256/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            }

            if (Enum.IsDefined(typeof(GenderDependent), pkm.Species) && !canGmax && pkm.Form is 0)
            {
                if (pkm.Gender == 0 && pkm.Species != (int)Species.Torchic)
                    md = true;
                else fd = true;
            }

            int form = pkm.Species switch
            {
                (int)Species.Sinistea or (int)Species.Polteageist or (int)Species.Rockruff or (int)Species.Mothim => 0,
                (int)Species.Alcremie when pkm.IsShiny || canGmax => 0,
                _ => pkm.Form,

            };

            if (pkm.Species is (ushort)Species.Sneasel)
            {
                if (pkm.Gender is 0)
                    md = true;
                else fd = true;
            }

            if (pkm.Species is (ushort)Species.Basculegion)
            {
                if (pkm.Gender is 0)
                {
                    md = true;
                    pkm.Form = 0;
                }
                else
                {
                    pkm.Form = 1;
                }

                string s = pkm.IsShiny ? "r" : "n";
                string g = md && pkm.Gender is not 1 ? "md" : "fd";
                return $"https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-HOME-Images/main/256x256/poke_capture_0" + $"{pkm.Species}" + "_00" + $"{pkm.Form}" + "_" + $"{g}" + "_n_00000000_f_" + $"{s}" + ".png";
            }

            baseLink[2] = pkm.Species < 10 ? $"000{pkm.Species}" : pkm.Species < 100 && pkm.Species > 9 ? $"00{pkm.Species}" : pkm.Species >= 1000 ? $"{pkm.Species}" : $"0{pkm.Species}";
            baseLink[3] = pkm.Form < 10 ? $"00{form}" : $"0{form}";
            baseLink[4] = pkm.PersonalInfo.OnlyFemale ? "fo" : pkm.PersonalInfo.OnlyMale ? "mo" : pkm.PersonalInfo.Genderless ? "uk" : fd ? "fd" : md ? "md" : "mf";
            baseLink[5] = canGmax ? "g" : "n";
            baseLink[6] = "0000000" + ((pkm.Species == (int)Species.Alcremie && !canGmax) ? ((IFormArgument)pkm).FormArgument.ToString() : "0");
            baseLink[8] = pkm.IsShiny ? "r.png" : "n.png";
            return string.Join("_", baseLink);
        }

        public static bool ShinyLockCheck(ushort species, string form, string ball = "")
        {
            if (ShinyLock.Contains(species))
                return true;
            else if (form is not "" && (species is (ushort)Species.Zapdos or (ushort)Species.Moltres or (ushort)Species.Articuno))
                return true;
            else if (ball.Contains("Beast") && (species is (ushort)Species.Poipole or (ushort)Species.Naganadel))
                return true;
            else if (typeof(T) == typeof(PB8) && (species is (ushort)Species.Manaphy or (ushort)Species.Mew or (ushort)Species.Jirachi))
                return true;
            else if (species is (ushort)Species.Pikachu && form is not "" && form is not "-Partner")
                return true;
            else if ((species is (ushort)Species.Zacian or (ushort)Species.Zamazenta) && !ball.Contains("Cherish"))
                return true;
            return false;
        }

        public static PKM TrashBytes(PKM pkm, LegalityAnalysis? la = null)
        {
            var pkMet = (T)pkm.Clone();
            if (pkMet.Version is not GameVersion.GO)
                pkMet.MetDate = DateOnly.FromDateTime(DateTime.Now);

            var analysis = new LegalityAnalysis(pkMet);
            var pkTrash = (T)pkMet.Clone();
            if (analysis.Valid)
            {
                pkTrash.IsNicknamed = true;
                pkTrash.Nickname = "UwU";
                pkTrash.SetDefaultNickname(la ?? new LegalityAnalysis(pkTrash));
            }

            if (new LegalityAnalysis(pkTrash).Valid)
                pkm = pkTrash;
            else if (analysis.Valid)
                pkm = pkMet;
            return pkm;
        }

        public static bool IsEggCheck(string showdownSet)
        {
            // Get the first line of the showdown set
            var firstLine = showdownSet.Split('\n').FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return false;
            }

            // Remove any text after '@' if present
            var atIndex = firstLine.IndexOf('@');
            if (atIndex > 0)
            {
                firstLine = firstLine[..atIndex].Trim();
            }

            // Split the remaining text into words
            var words = firstLine.Split(new[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if any word is exactly "Egg" (case-insensitive)
            return words.Any(word => string.Equals(word, "Egg", StringComparison.OrdinalIgnoreCase));
        }

        // Correct Met Dates for 7 Star Raids w/ Mightiest Mark
        private static readonly Dictionary<int, List<(DateOnly Start, DateOnly End)>> UnrivaledDateRanges = new()
        {
            // Generation 1
            [(int)Species.Charizard] = [(new(2022, 12, 02), new(2022, 12, 04)), (new(2022, 12, 16), new(2022, 12, 18)), (new(2024, 03, 13), new(2024, 03, 17))], // Charizard
            [(int)Species.Venusaur] = [(new(2024, 02, 28), new(2024, 03, 05))], // Venusaur
            [(int)Species.Blastoise] = [(new(2024, 03, 06), new(2024, 03, 12))], // Blastoise

            // Generation 2
            [(int)Species.Meganium] = [(new(2024, 04, 05), new(2024, 04, 07)), (new(2024, 04, 12), new(2024, 04, 14))], // Meganium
            [(int)Species.Typhlosion] = [(new(2023, 04, 14), new(2023, 04, 16)), (new(2023, 04, 21), new(2023, 04, 23))], // Typhlosion
            [(int)Species.Feraligatr] = [(new(2024, 11, 01), new(2024, 11, 03)), (new(2024, 11, 08), new(2024, 11, 10))], // Feraligatr
            [(int)Species.Porygon2] = [(new(2025, 06, 05), new(2025, 06, 15))], // Porygon2

            // Generation 3
            [(int)Species.Sceptile] = [(new(2024, 06, 28), new(2024, 06, 30)), (new(2024, 07, 05), new(2024, 07, 07))], // Sceptile
            [(int)Species.Blaziken] = [(new(2024, 01, 12), new(2024, 01, 14)), (new(2024, 01, 19), new(2024, 01, 21))], // Blaziken
            [(int)Species.Swampert] = [(new(2024, 05, 31), new(2024, 06, 02)), (new(2024, 06, 07), new(2024, 06, 09))], // Swampert

            // Generation 4
            [(int)Species.Empoleon] = [(new(2024, 02, 02), new(2024, 02, 04)), (new(2024, 02, 09), new(2024, 02, 11))], // Empoleon
            [(int)Species.Infernape] = [(new(2024, 10, 04), new(2024, 10, 06)), (new(2024, 10, 11), new(2024, 10, 13))],  // Infernape

            // Generation 5
            [(int)Species.Emboar] = [(new(2024, 06, 14), new(2024, 06, 16)), (new(2024, 06, 21), new(2024, 06, 23))], // Emboar
            [(int)Species.Serperior] = [(new(2024, 09, 20), new(2024, 09, 22)), (new(2024, 09, 27), new(2024, 09, 29))],  // Serperior

            // Generation 6
            [(int)Species.Chesnaught] = [(new(2023, 05, 12), new(2023, 05, 14)), (new(2023, 06, 16), new(2023, 06, 18))], // Chesnaught
            [(int)Species.Delphox] = [(new(2023, 07, 07), new(2023, 07, 09)), (new(2023, 07, 14), new(2023, 07, 16))], // Delphox

            // Generation 7
            [(int)Species.Decidueye] = [(new(2023, 03, 17), new(2023, 03, 19)), (new(2023, 03, 24), new(2023, 03, 26))], // Decidueye
            [(int)Species.Primarina] = [(new(2024, 05, 10), new(2024, 05, 12)), (new(2024, 05, 17), new(2024, 05, 19))], // Primarina
            [(int)Species.Incineroar] = [(new(2024, 09, 06), new(2024, 09, 08)), (new(2024, 09, 13), new(2024, 09, 15))], // Incineroar

            // Generation 8
            [(int)Species.Rillaboom] = [(new(2023, 07, 28), new(2023, 07, 30)), (new(2023, 08, 04), new(2023, 08, 06))], // Rillaboom
            [(int)Species.Cinderace] = [(new(2022, 12, 30), new(2023, 01, 01)), (new(2023, 01, 13), new(2023, 01, 15))], // Cinderace
            [(int)Species.Inteleon] = [(new(2023, 04, 28), new(2023, 04, 30)), (new(2023, 05, 05), new(2023, 05, 07))], // Inteleon

            // Others
            [(int)Species.Pikachu] = [(new(2023, 02, 24), new(2023, 02, 27)), (new(2024, 07, 12), new(2024, 07, 25))], // Pikachu
            [(int)Species.Eevee] = [(new(2023, 11, 17), new(2023, 11, 20))], // Eevee
            [(int)Species.Mewtwo] = [(new(2023, 09, 01), new(2023, 09, 17))], // Mewtwo
            [(int)Species.Greninja] = [(new(2023, 01, 27), new(2023, 01, 29)), (new(2023, 02, 10), new(2023, 02, 12))], // Greninja
            [(int)Species.Samurott] = [(new(2023, 03, 31), new(2023, 04, 02)), (new(2023, 04, 07), new(2023, 04, 09))], // Samurott
            [(int)Species.IronBundle] = [(new(2023, 12, 22), new(2023, 12, 24))], // Iron Bundle
            [(int)Species.Dondozo] = [(new(2024, 07, 26), new(2024, 08, 08))], // Dondozo
            [(int)Species.Dragonite] = [(new(2024, 08, 23), new(2024, 09, 01))], // Dragonite
            [(int)Species.Meowscarada] = [(new(2025, 02, 28), new(2025, 03, 06))], // Meowscarada
            [(int)Species.Skeledirge] = [(new(2025, 03, 06), new(2025, 03, 13))], // Skeledirge
            [(int)Species.Quaquaval] = [(new(2025, 03, 14), new(2025, 03, 20))], // Quaquaval
            [(int)Species.Tyranitar] = [(new(2025, 03, 28), new(2025, 03, 30)), (new(2025, 04, 04), new(2025, 04, 06))], // Tyranitar
            [(int)Species.Salamence] = [(new(2025, 04, 18), new(2025, 04, 20)), (new(2025, 04, 25), new(2025, 04, 27))], // Salamence
            [(int)Species.Metagross] = [(new(2025, 05, 09), new(2025, 05, 11)), (new(2025, 05, 12), new(2025, 05, 19))], // Metagross
            [(int)Species.Garchomp] = [(new(2025, 05, 22), new(2025, 05, 25)), (new(2025, 05, 30), new(2025, 06, 01))], // Garchomp
            [(int)Species.Baxcalibur] = [(new(2025, 06, 19), new(2025, 06, 22)), (new(2025, 06, 27), new(2025, 06, 29))], // Baxcalibur
        };

        public static void CheckAndSetUnrivaledDate(PKM pk)
        {
            if (pk is not IRibbonSetMark9 ribbonSetMark || !ribbonSetMark.RibbonMarkMightiest)
                return;

            List<(DateOnly Start, DateOnly End)> dateRanges;

            if (UnrivaledDateRanges.TryGetValue(pk.Species, out var ranges))
            {
                dateRanges = ranges;
            }
            else if (pk.Species is (int)Species.Decidueye or (int)Species.Typhlosion or (int)Species.Samurott && pk.Form == 1)
            {
                // Special handling for Hisuian forms
                dateRanges = pk.Species switch
                {
                    (int)Species.Decidueye => [(new(2023, 10, 06), new(2023, 10, 08)), (new(2023, 10, 13), new(2023, 10, 15))],
                    (int)Species.Typhlosion => [(new(2023, 11, 03), new(2023, 11, 05)), (new(2023, 11, 10), new(2023, 11, 12))],
                    (int)Species.Samurott => [(new(2023, 11, 24), new(2023, 11, 26)), (new(2023, 12, 01), new(2023, 12, 03))],
                    _ => []
                };
            }
            else
            {
                return;
            }

            if (!pk.MetDate.HasValue || !IsDateInRanges(pk.MetDate.Value, dateRanges))
            {
                SetRandomDateFromRanges(pk, dateRanges);
            }
        }

        private static bool IsDateInRanges(DateOnly date, List<(DateOnly Start, DateOnly End)> ranges)
            => ranges.Any(range => date >= range.Start && date <= range.End);

        private static void SetRandomDateFromRanges(PKM pk, List<(DateOnly Start, DateOnly End)> ranges)
        {
            var (Start, End) = ranges[Random.Shared.Next(ranges.Count)];
            int rangeDays = End.DayNumber - Start.DayNumber + 1;
            pk.MetDate = Start.AddDays(Random.Shared.Next(rangeDays));
        }
    

            public static byte DetectShowdownLanguage(string content)
        {
            // Check for batch command format: .Language=X
            var batchMatch = Regex.Match(content, @"\.Language=(\d+)");
            if (batchMatch.Success && byte.TryParse(batchMatch.Groups[1].Value, out byte langCode))
            {
                return langCode >= 1 && langCode <= 10 ? langCode : (byte)2; // Validate range and default to English if invalid
            }

            // Check for explicit language specification
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Language", StringComparison.OrdinalIgnoreCase))
                {
                    var language = line.Substring(line.IndexOf(':') + 1).Trim().ToLowerInvariant();

                    return language switch
                    {
                        "english" or "eng" or "en" => 2,
                        "french" or "français" or "fra" or "fr" => 3,
                        "italian" or "italiano" or "ita" or "it" => 4,
                        "german" or "deutsch" or "deu" or "de" => 5,
                        "spanish" or "español" or "spa" or "es" => 7,
                        "japanese" or "日本語" or "jpn" or "ja" => 1,
                        "korean" or "한국어" or "kor" or "ko" => 8,
                        "chinese simplified" or "中文简体" or "chs" or "zh-cn" => 9,
                        "chinese traditional" or "中文繁體" or "cht" or "zh-tw" => 10,
                        _ => 2 // Default to English if language is not recognized
                    };
                }
            }

            // French
            if (content.Contains("Talent") ||
                content.Contains("Bonheur") ||
                content.Contains("Chromatique") ||
                content.Contains("Niveau") ||
                content.Contains("Type Téra"))
            {
                return 3; // French
            }
            // Italian
            if (content.Contains("Abilità") ||
                content.Contains("Natura") ||
                content.Contains("Amicizia") ||
                content.Contains("Cromatico") ||
                content.Contains("Livello") ||
                content.Contains("Teratipo"))
            {
                return 4; // Italian
            }
            // German
            if (content.Contains("Fähigkeit") ||
                content.Contains("Wesen") ||
                content.Contains("Freundschaft") ||
                content.Contains("Schillerndes") ||
                content.Contains("Tera-Typ"))
            {
                return 5; // German
            }
            // Spanish
            if (content.Contains("Habilidad") ||
                content.Contains("Naturaleza") ||
                content.Contains("Felicidad") ||
                content.Contains("Teratipo"))
            {
                return 7; // Spanish
            }
            // Japanese
            if (content.Contains("特性") ||
                content.Contains("性格") ||
                content.Contains("なつき度") ||
                content.Contains("光ひかる") ||
                content.Contains("テラスタイプ"))
            {
                return 1; // Japanese
            }
            // Korean
            if (content.Contains("특성") ||
                content.Contains("성격") ||
                content.Contains("친밀도") ||
                content.Contains("빛나는") ||
                content.Contains("테라스탈타입"))
            {
                return 8; // Korean
            }
            // Chinese Simplified
            if (content.Contains("亲密度") ||
                content.Contains("异色") ||
                content.Contains("太晶属性"))
            {
                return 9; // Chinese Simplified
            }
            // Traditional Chinese
            if (content.Contains("親密度") ||
                content.Contains("發光寶") ||
                content.Contains("太晶屬性"))
            {
                return 10; // Chinese Traditional
            }
            // Default to English
            return 2;
        }
    } }


        // Add the missing method definition for 'SetHandlerandMemory' to the PKMExtensions class. 
        public static class PKMExtensions
        {
            public static void SetHandlerandMemory(this PKM pkm, ITrainerInfo trainerInfo, IEncounterable? encounter)
            {
                // Example implementation based on typical usage of handler and memory settings. 
                // Adjust the logic as per your application's requirements. 
                // Set the current handler to the trainer's ID. 
                pkm.CurrentHandler = 0;
                // Set the handling trainer's name and gender. 
                pkm.HandlingTrainerName = trainerInfo.OT;
                pkm.HandlingTrainerGender = trainerInfo.Gender;
                // If the encounter is not null, set additional memory details. 
                if (encounter != null)
                {
                    pkm.MetLocation = encounter.Location;
                    pkm.MetLevel = encounter.LevelMin;
                }
            }
        

        private static readonly Dictionary<string, int> BallIdMapping = new Dictionary<string, int>
        {
            {"Beast", 26},
            {"Cherish", 16},
            {"Dive", 7},
            {"Dream", 25},
            {"Dusk", 13},
            {"Fast", 17},
            {"Friend", 22},
            {"Great", 3},
            {"Heal", 14},
            {"Heavy", 20},
            {"Level", 18},
            {"Love", 21},
            {"Lure", 19},
            {"Luxury", 11},
            {"Master", 1},
            {"Moon", 23},
            {"Nest", 8},
            {"Net", 6},
            {"Poke", 4},
            {"Premier", 12},
            {"Quick", 15},
            {"Repeat", 9},
            {"Safari", 5},
            {"Sport", 24},
            {"Timer", 10},
            {"Ultra", 2},
            // LA Specific Balls
            {"LAPoke", 28},
            {"Strange", 27},
            {"LAGreat", 29},
            {"LAUltra", 30},
            {"Feather", 31},
            {"Wing", 32},
            {"Jet", 33},
            {"LAHeavy", 34},
            {"Leaden", 35},
            {"Gigaton", 36},
            {"Origin", 37}
        };
        public static string ConvertBalls(string content)
        {
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Ball:"))
                {
                    foreach (var ballMapping in BallIdMapping)
                    {
                        if (lines[i].Contains(ballMapping.Key))
                        {
                            lines[i] = $".Ball={ballMapping.Value}";
                            break;
                        }
                    }
                }
            }
            return string.Join('\n', lines);
        }
    }

