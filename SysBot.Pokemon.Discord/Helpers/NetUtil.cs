using Discord;
using PKHeX.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class NetUtil
{
    public static async Task<byte[]> DownloadFromUrlAsync(string url)
    {
        using var client = new HttpClient();
        return await client.GetByteArrayAsync(url).ConfigureAwait(false);
    }

    // add wondercard trading - thanks manu
    public static async Task<Download<PKM>> DownloadPKMAsync(IAttachment att, SimpleTrainerInfo? defTrainer = null)
    {
        var result = new Download<PKM> { SanitizedFileName = Format.Sanitize(att.Filename) };
        var extension = System.IO.Path.GetExtension(result.SanitizedFileName);
        var isMyg = MysteryGift.IsMysteryGift(att.Size) && extension != ".pb7";

        if (!EntityDetection.IsSizePlausible(att.Size) && !isMyg)
        {
            result.ErrorMessage = $"{result.SanitizedFileName}: Invalid size.";
            return result;
        }

        string url = att.Url;

        // Download the resource and load the bytes into a buffer.
        var buffer = await DownloadFromUrlAsync(url).ConfigureAwait(false);

        PKM? pkm = null;
        try
        {
            if (isMyg)
            {
                pkm = MysteryGift.GetMysteryGift(buffer, extension)?.ConvertToPKM(defTrainer ?? new SimpleTrainerInfo());
            }
            else
            {
                pkm = EntityFormat.GetFromBytes(buffer, EntityFileExtension.GetContextFromExtension(result.SanitizedFileName, EntityContext.None));
            }
        }
        catch (ArgumentException)
        {
            //Item wondercard
        }

        if (pkm is null)
        {
            result.ErrorMessage = $"{result.SanitizedFileName}: Invalid pkm attachment.";
            return result;
        }

        result.Data = pkm;
        result.Success = true;
        return result;
    }
}

public sealed class Download<T> where T : class
{
    public bool Success;
    public T? Data;
    public string? SanitizedFileName;
    public string? ErrorMessage;
}
