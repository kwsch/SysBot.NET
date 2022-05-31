using PKHeX.Core;
using System.Collections.Generic;
using System.IO;

namespace SysBot.Pokemon
{
    public static class LoadUtil
    {
        public static IEnumerable<string> GetFilesOfSize(IEnumerable<string> files, int size)
        {
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.Length == size)
                    yield return file;
            }
        }

        public static IEnumerable<T> GetPKMFilesOfType<T>(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var data = File.ReadAllBytes(file);
                var prefer = EntityFileExtension.GetContextFromExtension(file, EntityContext.None);
                var pkm = EntityFormat.GetFromBytes(data, prefer);
                if (pkm is T dest)
                    yield return dest;
            }
        }
    }
}