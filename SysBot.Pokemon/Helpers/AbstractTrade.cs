using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace SysBot.Pokemon.Helpers
{
    /// <summary>
    /// Pokémon abstract transaction class
    /// This class needs to implement SendMessage and also implement a multi-parameter constructor.
    /// The parameters should include information about the message sent by this type of robot so that SendMessage can be used
    /// Note that SetPokeTradeTrainerInfo and SetTradeQueueInfo must be called in the constructor of the abstract class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractTrade<T> where T : PKM, new()
    {
        public abstract void SendMessage(string message);//完善此方法以实现发送消息功能
        public abstract IPokeTradeNotifier<T> GetPokeTradeNotifier(T pkm, int code);//完善此方法以实现消息通知功能
        protected PokeTradeTrainerInfo userInfo = default!;
        private TradeQueueInfo<T> queueInfo = default!;
        private List<pictocodes> lgcode;
        public static readonly ushort[] ShinyLock = {  (ushort)Species.Victini, (ushort)Species.Keldeo, (ushort)Species.Volcanion, (ushort)Species.Cosmog, (ushort)Species.Cosmoem, (ushort)Species.Magearna, (ushort)Species.Marshadow, (ushort)Species.Eternatus,
                                                    (ushort)Species.Kubfu, (ushort)Species.Urshifu, (ushort)Species.Zarude, (ushort)Species.Glastrier, (ushort)Species.Spectrier, (ushort)Species.Calyrex };

        public void SetPokeTradeTrainerInfo(PokeTradeTrainerInfo pokeTradeTrainerInfo)
        {
            userInfo = pokeTradeTrainerInfo;
        }

        public void SetTradeQueueInfo(TradeQueueInfo<T> queueInfo)
        {
            this.queueInfo = queueInfo;
        }

        public static bool HasAdName(T pk, out string ad)
        {
            string pattern = @"(YT$)|(YT\w*$)|(Lab$)|(\.\w*$|\.\w*\/)|(TV$)|(PKHeX)|(FB:)|(AuSLove)|(ShinyMart)|(Blainette)|(\ com)|(\ org)|(\ net)|(2DOS3)|(PPorg)|(Tik\wok$)|(YouTube)|(IG:)|(TTV\ )|(Tools)|(JokersWrath)|(bot$)|(PKMGen)|(TheHighTable)";
            bool ot = Regex.IsMatch(pk.OT_Name, pattern, RegexOptions.IgnoreCase);
            bool nick = Regex.IsMatch(pk.Nickname, pattern, RegexOptions.IgnoreCase);
            ad = ot ? pk.OT_Name : nick ? pk.Nickname : "";
            return ot || nick;
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

        public static string PokeImg(PKM pkm, bool canGmax, bool fullSize)
        {
            bool md = false;
            bool fd = false;
            string[] baseLink;
            if (fullSize)
                baseLink = "https://raw.githubusercontent.com/bdawg1989/HomeImages/master/512x512/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            else baseLink = "https://raw.githubusercontent.com/bdawg1989/HomeImages/master/128x128/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');

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
                    pkm.Form = 1;

                string s = pkm.IsShiny ? "r" : "n";
                string g = md && pkm.Gender is not 1 ? "md" : "fd";
                return $"https://raw.githubusercontent.com/bdawg1989/HomeImages/master/128x128/poke_capture_0" + $"{pkm.Species}" + "_00" + $"{pkm.Form}" + "_" + $"{g}" + "_n_00000000_f_" + $"{s}" + ".png";
            }

            baseLink[2] = pkm.Species < 10 ? $"000{pkm.Species}" : pkm.Species < 100 && pkm.Species > 9 ? $"00{pkm.Species}" : pkm.Species >= 1000 ? $"{pkm.Species}" : $"0{pkm.Species}";
            baseLink[3] = pkm.Form < 10 ? $"00{form}" : $"0{form}";
            baseLink[4] = pkm.PersonalInfo.OnlyFemale ? "fo" : pkm.PersonalInfo.OnlyMale ? "mo" : pkm.PersonalInfo.Genderless ? "uk" : fd ? "fd" : md ? "md" : "mf";
            baseLink[5] = canGmax ? "g" : "n";
            baseLink[6] = "0000000" + (pkm.Species == (int)Species.Alcremie && !canGmax ? pkm.Data[0xE4] : 0);
            baseLink[8] = pkm.IsShiny ? "r.png" : "n.png";
            return string.Join("_", baseLink);
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

        public static PKM TrashBytes(PKM pkm, LegalityAnalysis? la = null)
        {
            var pkMet = (T)pkm.Clone();
            if (pkMet.Version is not (int)GameVersion.GO)
                pkMet.MetDate = DateOnly.Parse("2020/10/20");

            var analysis = new LegalityAnalysis(pkMet);
            var pkTrash = (T)pkMet.Clone();
            if (analysis.Valid)
            {
                pkTrash.IsNicknamed = true;
                pkTrash.Nickname = "KOIKOIKOIKOI";
                pkTrash.SetDefaultNickname(la ?? new LegalityAnalysis(pkTrash));
            }

            if (new LegalityAnalysis(pkTrash).Valid)
                pkm = pkTrash;
            else if (analysis.Valid)
                pkm = pkMet;
            return pkm;
        }

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
            else return new();

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
            else return (T)mgPkm;
        }

        public void StartTradePs(string ps)
        {
            var _ = CheckAndGetPkm(ps, out var msg, out var pkm);
            if (!_)
            {
                SendMessage(msg);
                return;
            }
            var foreign = ps.Contains("Language: ");
            StartTradeWithoutCheck(pkm, foreign);
        }
        public void StartTradeChinesePs(string chinesePs)
        {
            var ps = ShowdownTranslator<T>.Chinese2Showdown(chinesePs);
            LogUtil.LogInfo($"PS code after Chinese conversion:\n{ps}", nameof(AbstractTrade<T>));
            StartTradePs(ps);
        }
        public void StartTradePKM(T pkm)
        {
            var _ = CheckPkm(pkm, out var msg);
            if (!_)
            {
                SendMessage(msg);
                return;
            }

            StartTradeWithoutCheck(pkm);
        }

        public void StartTradeMultiPs(string pss)
        {
            var psList = pss.Split("\n\n").ToList();
            if (!JudgeMultiNum(psList.Count)) return;

            var pkms = GetPKMsFromPsList(psList, isChinesePS: false, out int invalidCount, out List<bool> skipAutoOTList);

            if (!JudgeInvalidCount(invalidCount, psList.Count)) return;

            var code = queueInfo.GetRandomTradeCode();
            var __ = AddToTradeQueue(pkms, code, skipAutoOTList,
                PokeRoutineType.LinkTrade, out string message);
            SendMessage(message);
        }

        public void StartTradeMultiChinesePs(string chinesePssString)
        {
            var chinesePsList = chinesePssString.Split('+').ToList();
            if (!JudgeMultiNum(chinesePsList.Count)) return;

            List<T> pkms = GetPKMsFromPsList(chinesePsList, true, out int invalidCount, out List<bool> skipAutoOTList);

            if (!JudgeInvalidCount(invalidCount, chinesePsList.Count)) return;

            var code = queueInfo.GetRandomTradeCode();
            var __ = AddToTradeQueue(pkms, code, skipAutoOTList,
                PokeRoutineType.LinkTrade, out string message);
            SendMessage(message);
        }

        public void StartTradeMultiPKM(List<T> rawPkms)
        {
            if (!JudgeMultiNum(rawPkms.Count)) return;

            List<T> pkms = new();
            List<bool> skipAutoOTList = new();
            int invalidCount = 0;
            for (var i = 0; i < rawPkms.Count; i++)
            {
                var _ = CheckPkm(rawPkms[i], out var msg);
                if (!_)
                {
                    LogUtil.LogInfo($"There is a problem with the {i + 1}th Pokémon in the batch:{msg}", nameof(AbstractTrade<T>));
                    invalidCount++;
                }
                else
                {
                    LogUtil.LogInfo($"The {i + 1}th batch: {GameInfo.GetStrings("en").Species[rawPkms[i].Species]}", nameof(AbstractTrade<T>));
                    skipAutoOTList.Add(false);
                    pkms.Add(rawPkms[i]);
                }
            }

            if (!JudgeInvalidCount(invalidCount, rawPkms.Count)) return;

            var code = queueInfo.GetRandomTradeCode();
            var __ = AddToTradeQueue(pkms, code, skipAutoOTList,
                PokeRoutineType.LinkTrade, out string message);
            SendMessage(message);
        }
        /// <summary>
        /// Generate the corresponding version of the PKM file based on the pokemon showdown code
        /// </summary>
        /// <param name="psList">ps code</param>
        /// <param name="isChinesePS">Is it Chinese ps</param>
        /// <param name="invalidCount">Number of illegal Pokémon</param>
        /// <param name="skipAutoOTList">List that needs to skip self-id</param>
        /// <returns></returns>
        private List<T> GetPKMsFromPsList(List<string> psList, bool isChinesePS, out int invalidCount, out List<bool> skipAutoOTList)
        {
            List<T> pkms = new();
            skipAutoOTList = new List<bool>();
            invalidCount = 0;
            for (var i = 0; i < psList.Count; i++)
            {
                var ps = isChinesePS ? ShowdownTranslator<T>.Chinese2Showdown(psList[i]) : psList[i];
                var _ = CheckAndGetPkm(ps, out var msg, out var pkm);
                if (!_)
                {
                    LogUtil.LogInfo($"There is a problem with the {i + 1}th Pokémon in the batch:{msg}", nameof(AbstractTrade<T>));
                    invalidCount++;
                }
                else
                {
                    LogUtil.LogInfo($"PS code after Chinese conversion:\n{ps}", nameof(AbstractTrade<T>));
                    skipAutoOTList.Add(ps.Contains("Language: "));
                    pkms.Add(pkm);
                }
            }
            return pkms;
        }
        /// <summary>
        /// 判断是否符合批量规则
        /// </summary>
        /// <param name="multiNum">待计算的数量</param>
        /// <returns></returns>
        private bool JudgeMultiNum(int multiNum)
        {
            var maxPkmsPerTrade = queueInfo.Hub.Config.Trade.MaxPkmsPerTrade;
            if (maxPkmsPerTrade <= 1)
            {
                SendMessage("Please contact the bot owner to change the trade/MaxPkmsPerTrade configuration to greater than 1.");
                return false;
            }
            else if (multiNum > maxPkmsPerTrade)
            {
                SendMessage($"The number of Pokémon exchanged in batches should be less than or equal to {maxPkmsPerTrade}.");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 判断无效数量
        /// </summary>
        /// <param name="invalidCount"></param>
        /// <param name="totalCount"></param>
        /// <returns></returns>
        private bool JudgeInvalidCount(int invalidCount, int totalCount)
        {
            if (invalidCount == totalCount)
            {
                SendMessage("None of them are legal, try again.");
                return false;
            }
            else if (invalidCount != 0)
            {
                SendMessage($"Among the {totalCount} Pokémon expected to be traded, {invalidCount} are illegal. Only {totalCount - invalidCount} legal ones will be traded.");
            }
            return true;
        }

        public void StartTradeWithoutCheck(T pkm, bool foreign = false)
        {
            var code = queueInfo.GetRandomTradeCode();
            var __ = AddToTradeQueue(pkm, code, foreign,
                PokeRoutineType.LinkTrade, out string message);
            SendMessage(message);
        }

        public void StartDump()
        {
            var code = queueInfo.GetRandomTradeCode();
            var __ = AddToTradeQueue(new T(), code, false,
                PokeRoutineType.Dump, out string message);
            SendMessage(message);
        }

        public bool Check(T pkm, out string msg)
        {
            try
            {
                if (!pkm.CanBeTraded())
                {
                    msg = $"Cancelling trade, trading of this Pokémon is prohibited!";
                    return false;
                }
                if (pkm is T pk)
                {
                    var la = new LegalityAnalysis(pkm);
                    var valid = la.Valid;
                    if (valid)
                    {
                        msg = $"Already added to the waiting queue. If you select a Pokémon too slowly, your delivery request will be cancelled!";
                        return true;
                    }
                    LogUtil.LogInfo($"Illegal reason:\n{la.Report()}", nameof(AbstractTrade<T>));
                }
                LogUtil.LogInfo($"pkm type:{pkm.GetType()}, T:{typeof(T)}", nameof(AbstractTrade<T>));
                var reason = "I can't create illegal Pokémon.";
                msg = $"{reason}";
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(AbstractTrade<T>));
                msg = $"Cancel delivery, an error occurred.";
            }
            return false;
        }
        public bool CheckPkm(T pkm, out string msg)
        {
            if (!queueInfo.GetCanQueue())
            {
                msg = "Sorry, I'm not accepting requests!";
                return false;
            }
            return Check(pkm, out msg);
        }

        public bool CheckAndGetPkm(string setstring, out string msg, out T outPkm)
        {
            outPkm = new T();
            if (!queueInfo.GetCanQueue())
            {
                msg = "Sorry, I'm not accepting requests!";
                return false;
            }

            var set = ShowdownUtil.ConvertToShowdown(setstring);
            if (set == null)
            {
                msg = $"The delivery is cancelled, and the Pokémon's nickname is empty.";
                return false;
            }

            var template = AutoLegalityWrapper.GetTemplate(set);
            if (template.Species < 1)
            {
                msg =
                    $"To cancel delivery, please use the correct Showdown Set code";
                return false;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                GenerationFix(sav);
                var pkm = sav.GetLegal(template, out var result);
                if (pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species)) EggTrade(pkm, template);
                if (Check((T)pkm, out msg))
                {
                    outPkm = (T)pkm;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(AbstractTrade<T>));
                msg = $"Cancel delivery, an error occurred.";
            }

            return false;
        }

        private static void GenerationFix(ITrainerInfo sav)
        {
            if (typeof(T) == typeof(PK8) || typeof(T) == typeof(PB8) || typeof(T) == typeof(PA8)) sav.GetType().GetProperty("Generation")?.SetValue(sav, 8);
        }

        private bool AddToTradeQueue(T pk, int code, bool skipAutoOT,
            PokeRoutineType type, out string msg)
        {
            return AddToTradeQueue(new List<T> { pk }, code, new List<bool> { skipAutoOT }, type, out msg);
        }

        private bool AddToTradeQueue(List<T> pks, int code, List<bool> skipAutoOTList,
            PokeRoutineType type, out string msg)
        {
            if (pks == null || pks.Count == 0)
            {
                msg = $"Pokémon data is empty.";
                return false;
            }
            T pk = pks.First();
            var trainer = userInfo;
            var notifier = GetPokeTradeNotifier(pk, code);
            var tt = type == PokeRoutineType.SeedCheck
                ? PokeTradeType.Seed
                : (type == PokeRoutineType.Dump ? PokeTradeType.Dump : PokeTradeType.Specific);
            var detail =
                new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, lgcode, true);
            detail.Context.Add("skipAutoOTList", skipAutoOTList);
            if (pks.Count > 0)
            {
                detail.Context.Add("batch", pks);
            }
            var trade = new TradeEntry<T>(detail, userInfo.ID, type, userInfo.TrainerName);

            var added = queueInfo.AddToTradeQueue(trade, userInfo.ID, false);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"You are already in the queue, please do not send again.";
                return false;
            }

            var position = queueInfo.CheckPosition(userInfo.ID, type);
            //msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";
            msg = $"Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";

            var botct = queueInfo.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = queueInfo.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                //msg += $". Estimated: {eta:F1} minutes.";
                msg += $". Estimated: {eta:F1} minutes.";
            }

            return true;
        }

        public static void DittoTrade(PKM pkm)
        {
            var dittoStats = new string[] { "atk", "spe", "spa" };
            var nickname = pkm.Nickname.ToLower();
            pkm.StatNature = pkm.Nature;
            pkm.Met_Location = pkm switch
            {
                PB8 => 400,
                PK9 => 28,
                _ => 162, // PK8
            };

            pkm.Met_Level = pkm switch
            {
                PB8 => 29,
                PK9 => 34,
                _ => pkm.Met_Level,
            };

            if (pkm is PK9 pk9)
            {
                pk9.Obedience_Level = (byte)pk9.Met_Level;
                pk9.TeraTypeOriginal = MoveType.Normal;
                pk9.TeraTypeOverride = (MoveType)19;
            }

            pkm.Ball = 21;
            pkm.IVs = new int[] { 31, nickname.Contains(dittoStats[0]) ? 0 : 31, 31, nickname.Contains(dittoStats[1]) ? 0 : 31, nickname.Contains(dittoStats[2]) ? 0 : 31, 31 };
            pkm.ClearHyperTraining();
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
                    _ => "Egg",
                };
            }
            else
            {
                pk.IsNicknamed = false;
                pk.Nickname = "";
            }

            pk.IsEgg = true;
            pk.Egg_Location = pk switch
            {
                PB8 => 60010,
                PK9 => 30023,
                _ => 60002, //PK8
            };

            pk.HeldItem = 0;
            pk.CurrentLevel = 1;
            pk.EXP = 0;
            pk.Met_Level = 1;
            pk.Met_Location = pk switch
            {
                PB8 => 65535,
                PK9 => 0,
                _ => 30002, //PK8
            };

            pk.CurrentHandler = 0;
            pk.OT_Friendship = 1;
            pk.HT_Name = "";
            pk.HT_Friendship = 0;
            pk.ClearMemories();
            pk.StatNature = pk.Nature;
            pk.SetEVs(new int[] { 0, 0, 0, 0, 0, 0 });

            MarkingApplicator.SetMarkings(pk);

            pk.ClearRelearnMoves();

            if (pk is PK8 pk8)
            {
                pk8.HT_Language = 0;
                pk8.HT_Gender = 0;
                pk8.HT_Memory = 0;
                pk8.HT_Feeling = 0;
                pk8.HT_Intensity = 0;
                pk8.DynamaxLevel = pk8.GetSuggestedDynamaxLevel(pk8, 0);
            }
            else if (pk is PB8 pb8)
            {
                pb8.HT_Language = 0;
                pb8.HT_Gender = 0;
                pb8.HT_Memory = 0;
                pb8.HT_Feeling = 0;
                pb8.HT_Intensity = 0;
                pb8.DynamaxLevel = pb8.GetSuggestedDynamaxLevel(pb8, 0);
            }
            else if (pk is PK9 pk9)
            {
                pk9.HT_Language = 0;
                pk9.HT_Gender = 0;
                pk9.HT_Memory = 0;
                pk9.HT_Feeling = 0;
                pk9.HT_Intensity = 0;
                pk9.Obedience_Level = 1;
                pk9.Version = 0;
                pk9.BattleVersion = 0;
                pk9.TeraTypeOverride = (MoveType)19;
            }

            var la = new LegalityAnalysis(pk);
            var enc = la.EncounterMatch;
            pk.CurrentFriendship = enc is IHatchCycle s ? s.EggCycles : pk.PersonalInfo.HatchCycles;

            Span<ushort> relearn = stackalloc ushort[4];
            la.GetSuggestedRelearnMoves(relearn, enc);
            pk.SetRelearnMoves(relearn);

            pk.SetSuggestedMoves();

            pk.Move1_PPUps = pk.Move2_PPUps = pk.Move3_PPUps = pk.Move4_PPUps = 0;
            pk.SetMaximumPPCurrent(pk.Moves);
            pk.SetSuggestedHyperTrainingData();
            pk.SetSuggestedRibbons(template, enc, true);
        }

    }
}
