using PKHeX.Core;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon
{
    public class ShowdownTranslator<T> where T : PKM
    {
        public static GameStrings GameStringsZh = GameInfo.GetStrings("zh");
        public static GameStrings GameStringsEn = GameInfo.GetStrings("en");
        public static string Chinese2Showdown(string zh)
        {
            string result = "";

            // 添加宝可梦
            int specieNo = GameStringsZh.Species.Skip(1).Select((s, index) => new { Species = s, Index = index + 1 })
                .Where(s => zh.Contains(s.Species)).OrderByDescending(s => s.Species.Length).FirstOrDefault()?.Index ?? -1;

            if (specieNo <= 0) return result;
            result = specieNo switch
            {
                (int)Species.NidoranF => "Nidoran-F",
                (int)Species.NidoranM => "Nidoran-M",
                _ => GameStringsEn.Species[specieNo],
            };

            zh = zh.Replace(GameStringsZh.Species[specieNo], "");

            // 特殊性别差异
            // 29-尼多兰F，32-尼多朗M，678-超能妙喵F，876-爱管侍F，902-幽尾玄鱼F, 916-飘香豚
            if (((Species)specieNo is Species.Meowstic or Species.Indeedee or Species.Basculegion or Species.Oinkologne)
                && zh.Contains("母")) result += "-F";


            // 识别地区形态
            foreach (var s in ShowdownTranslatorDictionary.formDict)
            {
                var searchKey = s.Key.EndsWith("形态") ? s.Key : s.Key + "形态";
                if (!zh.Contains(searchKey)) continue;
                result += $"-{s.Value}";
                zh = zh.Replace(searchKey, "");
                break;
            }

            // 识别蛋
            if (zh.Contains("的蛋"))
            {
                result = $"Egg ({result})";
                zh = zh.Replace("的蛋", "");
            }

            // 添加性别
            if (zh.Contains("公"))
            {
                result += " (M)";
                zh = zh.Replace("公", "");
            }
            else if (zh.Contains("母"))
            {
                result += " (F)";
                zh = zh.Replace("母", "");
            }

            // 添加持有物
            foreach (var holdItemKeyword in ShowdownTranslatorDictionary.holdItemKeywords)
            {
                if (!zh.Contains(holdItemKeyword)) continue;
                for (int i = 1; i < GameStringsZh.Item.Count; i++)
                {
                    if (GameStringsZh.Item[i].Length == 0) continue;
                    if (!zh.Contains(holdItemKeyword + GameStringsZh.Item[i])) continue;
                    result += $" @ {GameStringsEn.Item[i]}";
                    zh = zh.Replace(holdItemKeyword + GameStringsZh.Item[i], "");
                    break;
                }
            }

            // 添加等级
            if (Regex.IsMatch(zh, "\\d{1,3}级"))
            {
                string level = Regex.Match(zh, "(\\d{1,3})级").Groups?[1]?.Value ?? "100";
                result += $"\nLevel: {level}";
                zh = Regex.Replace(zh, "\\d{1,3}级", "");
            }

            // 添加超极巨化
            if (typeof(T) == typeof(PK8) && zh.Contains("超极巨"))
            {
                result += "\nGigantamax: Yes";
                zh = zh.Replace("超极巨", "");
            }

            // 添加异色
            foreach (string key in ShowdownTranslatorDictionary.shinyTypes.Keys)
            {
                if (zh.Contains(key))
                {
                    result += ShowdownTranslatorDictionary.shinyTypes[key];
                    zh = zh.Replace(key, "");
                    break;
                }
            }

            // 添加头目
            if (typeof(T) == typeof(PA8) && zh.Contains("头目"))
            {
                result += "\nAlpha: Yes";
                zh = zh.Replace("头目", "");
            }

            // 添加球种
            for (int i = 1; i < GameStringsZh.balllist.Length; i++)
            {
                if (GameStringsZh.balllist[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.balllist[i])) continue;
                var ballStr = GameStringsEn.balllist[i];
                if (typeof(T) == typeof(PA8) && ballStr is "Poké Ball" or "Great Ball" or "Ultra Ball") ballStr = "LA" + ballStr;
                result += $"\nBall: {ballStr}";
                zh = zh.Replace(GameStringsZh.balllist[i], "");
                break;
            }

            // 添加特性
            for (int i = 1; i < GameStringsZh.Ability.Count; i++)
            {
                if (GameStringsZh.Ability[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.Ability[i] + "特性")) continue;
                result += $"\nAbility: {GameStringsEn.Ability[i]}";
                zh = zh.Replace(GameStringsZh.Ability[i] + "特性", "");
                break;
            }

            // 添加性格
            for (int i = 0; i < GameStringsZh.Natures.Count; i++)
            {
                if (GameStringsZh.Natures[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.Natures[i])) continue;
                result += $"\n{GameStringsEn.Natures[i]} Nature";
                zh = zh.Replace(GameStringsZh.Natures[i], "");
                break;
            }

            // 添加个体值
            foreach (string key in ShowdownTranslatorDictionary.ivCombos.Keys)
            {
                if (zh.ToUpper().Contains(key))
                {
                    result += "\nIVs: " + ShowdownTranslatorDictionary.ivCombos[key];
                    zh = Regex.Replace(zh, key, "", RegexOptions.IgnoreCase);
                    break;
                }
            }

            // 添加努力值
            if (zh.Contains("努力值"))
            {
                StringBuilder sb = new();
                sb.Append("\nEVs: ");
                zh = zh.Replace("努力值", "");

                foreach (var stat in ShowdownTranslatorDictionary.statsDict)
                {
                    string regexPattern = $@"\d{{1,3}}{stat.Key}";
                    if (Regex.IsMatch(zh, regexPattern))
                    {
                        string value = Regex.Match(zh, $@"(\d{{1,3}}){stat.Key}").Groups[1].Value;
                        sb.Append($"{value} {stat.Value} / ");
                        zh = Regex.Replace(zh, regexPattern, "");
                    }
                    else if (Regex.IsMatch(zh, $@"\d{{1,3}}{stat.Value}"))
                    {
                        string value = Regex.Match(zh, $@"(\d{{1,3}}){stat.Value}").Groups?[1]?.Value ?? "";
                        sb.Append($"{value} {stat.Value} / ");
                        zh = Regex.Replace(zh, $@"\d{{1,3}}{stat.Value}", "");
                    }
                }

                if (sb.ToString().EndsWith("/ "))
                {
                    sb.Remove(sb.Length - 2, 2);
                }

                result += sb.ToString();
            }

            // 添加太晶属性
            if (typeof(T) == typeof(PK9))
            {
                for (int i = 0; i < GameStringsZh.Types.Count; i++)
                {
                    if (GameStringsZh.Types[i].Length == 0) continue;
                    if (!zh.Contains("太晶" + GameStringsZh.Types[i])) continue;
                    result += $"\nTera Type: {GameStringsEn.Types[i]}";
                    zh = zh.Replace("太晶" + GameStringsZh.Types[i], "");
                    break;
                }
            }

            // 补充后天获得的全奖章 注意开启Legality=>AllowBatchCommands
            if (typeof(T) == typeof(PK9) && zh.Contains("全奖章"))
            {
                result += "\n.Ribbons=$suggestAll\n.RibbonMarkPartner=True\n.RibbonMarkGourmand=True";
                zh = zh.Replace("全奖章", "");
            }
            // 体型大小并添加证章
            if (typeof(T) == typeof(PK9) && zh.Contains("大个子"))
            {
                result += $"\n.Scale=255\n.RibbonMarkJumbo=True";
                zh = zh.Replace("大个子", "");
            }
            else if (typeof(T) == typeof(PK9) && zh.Contains("小不点"))
            {
                result += $"\n.Scale=0\n.RibbonMarkMini=True";
                zh = zh.Replace("小不点", "");
            }

            //添加全回忆技能
            if (typeof(T) == typeof(PK9) || typeof(T) == typeof(PK8))
            {
                if (Regex.IsMatch(zh, "全技能|全招式"))
                {
                    result += "\n.RelearnMoves=$suggestAll";
                    zh = Regex.Replace(zh, "全技能|全招式", "");
                }
            }
            else if (typeof(T) == typeof(PA8))
            {
                if (Regex.IsMatch(zh, "全技能|全招式"))
                {
                    result += "\n.MoveMastery=$suggestAll";
                    zh = Regex.Replace(zh, "全技能|全招式", "");
                }
            }

            // 语言
            var lang = ShowdownTranslatorDictionary.languages.Keys.FirstOrDefault(zh.Contains);
            if (!string.IsNullOrEmpty(lang))
            {
                result += $"\nLanguage: {ShowdownTranslatorDictionary.languages[lang]}";
                zh = zh.Replace(lang, "");
            }

            // 添加技能 原因：PKHeX.Core.ShowdownSet#ParseLines中，若招式数满足4个则不再解析，所以招式文本应放在最后
            for (int moveCount = 0; moveCount < 4; moveCount++)
            {
                var candidateIndex = GameStringsZh.Move.Select((move, index) => new { Move = move, Index = index })
                    .Where(move => move.Move.Length > 0 && zh.Contains("-" + move.Move))
                    .OrderByDescending(move => move.Move.Length).FirstOrDefault()?.Index ?? -1;
                if (candidateIndex < 0) continue;
                result += $"\n-{GameStringsEn.Move[candidateIndex]}";
                zh = zh.Replace("-" + GameStringsZh.Move[candidateIndex], "");
            }

            return result;
        }

        public static bool IsPS(string str) => GameStringsEn.Species.Skip(1).Any(str.Contains);

    }
}
