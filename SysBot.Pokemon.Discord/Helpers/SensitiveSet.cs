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

        // ReSharper disable once StaticMemberInGenericType
        private static readonly string[] Splitters = {", ", ","};

        private static IEnumerable<T> Convert(string str, Func<string, T> parse)
        {
            var split = str.Split(Splitters, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in split)
            {
                // Since the input can be user entered, just try-catch the conversion.
                T c; try { c = parse(item); }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
                yield return c;
            }
        }

        /// <summary>
        /// Loads the list of <see cref="SensitiveSet{T}"/> items from the input string, using the provided conversion function.
        /// </summary>
        /// <param name="str">String containing the list of items</param>
        /// <param name="parse">Converts the split elements into <see cref="T"/> items.</param>
        public void Read(string str, Func<string, T> parse)
        {
            var list = Convert(str, parse);
            foreach (var item in list)
                List.Add(item);
        }
    }
}