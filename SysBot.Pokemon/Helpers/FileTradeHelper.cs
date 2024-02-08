using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Helpers
{
    /// <summary>
    /// 宝可梦文件交易帮助类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FileTradeHelper<T> where T : PKM, new()
    {
        /// <summary>
        /// 将bin文件转换成对应版本的PKM list
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static List<T> Bin2List(byte[] bb)
        {
            if (pkmSize[typeof(T)] == bb.Length)
            {
                var tp = GetPKM(bb);
                if (tp != null && tp.Species > 0 && tp.Valid && tp is T pkm) return new List<T>() { pkm };
            }
            int size = pkmSizeInBin[typeof(T)];
            int times = bb.Length % size == 0 ? (bb.Length / size) : (bb.Length / size + 1);
            List<T> pkmBytes = new();
            for (var i = 0; i < times; i++)
            {
                int start = i * size;
                int end = (start + size) > bb.Length ? bb.Length : (start + size);
                var tp = GetPKM(bb[start..end]);
                if (tp != null && tp.Species > 0 && tp.Valid && tp is T pkm) pkmBytes.Add(pkm);
            }
            return pkmBytes;
        }
        /// <summary>
        /// 文件名称是否有效
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool ValidFileName(string fileName)
        {
            string ext = fileName?.Split('.').Last().ToLower() ?? "";
            return (ext == typeof(T).Name.ToLower()) || (ext == "bin");
        }
        /// <summary>
        /// 文件大小是否有效
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool ValidFileSize(long size) => ValidPKMFileSize(size) || ValidBinFileSize(size);
        public static bool ValidPKMFileSize(long size) => size == pkmSize[typeof(T)];
        public static bool ValidBinFileSize(long size) => (size > 0) && (size <= MaxCountInBin * pkmSizeInBin[typeof(T)]) && (size % pkmSizeInBin[typeof(T)] == 0);
        public static int MaxCountInBin => maxCountInBin[typeof(T)];

        static PKM? GetPKM(byte[] ba) => typeof(T) switch
        {
            Type t when t == typeof(PK8) => new PK8(ba),
            Type t when t == typeof(PB8) => new PB8(ba),
            Type t when t == typeof(PA8) => new PA8(ba),
            Type t when t == typeof(PK9) => new PK9(ba),
            _ => null
        };
        static readonly Dictionary<Type, int> pkmSize = new() 
        {
            { typeof(PK8), 344 },
            { typeof(PB8), 344 },
            { typeof(PA8), 376 },
            { typeof(PK9), 344 }
        };

        static readonly Dictionary<Type, int> pkmSizeInBin = new()
        {
            { typeof(PK8), 344 },
            { typeof(PB8), 344 },
            { typeof(PA8), 360 },
            { typeof(PK9), 344 }
        };

        static readonly Dictionary<Type, int> maxCountInBin = new()
        {
            { typeof(PK8), 960 },
            { typeof(PB8), 1200 },
            { typeof(PA8), 960 },
            { typeof(PK9), 960 }
        };

    }
}
