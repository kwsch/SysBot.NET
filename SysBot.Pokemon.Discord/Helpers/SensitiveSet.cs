using System;
using System.Collections.Generic;

namespace SysBot.Pokemon.Discord
{
    public class SensitiveSet<T>
    {
        private readonly HashSet<T> List = new HashSet<T>();
        public bool Add(T id) => !List.Contains(id) && List.Add(id);
        public bool Remove(T id) => List.Contains(id) && List.Remove(id);
        public bool Contains(T id) => List.Contains(id);
        public string Write() => string.Join(",", List);
        public int Count => List.Count;

        private static IEnumerable<T> Convert(string str, Func<string, T> converter)
        {
            var split = str.Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in split)
            {
                // Since the input can be user entered, just trycatch the conversion.
                T c; try { c = converter(item); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
                yield return c;
            }
        }

        public void Read(string configDiscordBlackList, Func<string, T> parse)
        {
            var list = Convert(configDiscordBlackList, parse);
            foreach (var item in list)
                List.Add(item);
        }
    }
}