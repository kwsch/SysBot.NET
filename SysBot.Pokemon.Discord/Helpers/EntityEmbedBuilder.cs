using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Discord;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public class EntityEmbedBuilder : EmbedBuilder
{
    private readonly PKM _entity;

    public EntityEmbedBuilder(PKM pk)
    {
        _entity = pk;
        Color = ((PersonalColor)pk.PersonalInfo.Color).ToDiscordColor();
        Title = "Pokémon Info";
        Timestamp = DateTime.UtcNow;
    }

    public EntityEmbedBuilder AddTradeCode(int tradeCode)
    {
        AddField(x =>
        {
            x.Name = "Trade Code:";
            x.Value = Format.Bold($"{tradeCode:0000 0000}");
            x.IsInline = true;
        });
        return this;
    }

    public EntityEmbedBuilder AddWaitTime(float minutes)
    {
        AddField(x =>
        {
            x.Name = "Estimated Wait:";
            x.Value = $"{minutes:F1} minutes.";
            x.IsInline = true;
        });
        return this;
    }

    public EntityEmbedBuilder AddReceiving()
    {
        if (_entity.Species == 0)
            return this;

        AddField(x =>
        {
            x.Name = "Receiving:";
            x.Value = ReusableActions.FormatSetCode(_entity);
            x.IsInline = true;
        });
        return this;
    }

    public void AddQueuePosition(int checkPosition) => Footer = new EmbedFooterBuilder
    {
        Text = $"Position: {checkPosition}"
    };

    public bool TryAddSpriteThumbnail([NotNullWhen(true)] out MemoryStream? sprite, [NotNullWhen(true)] out FileAttachment? thumb)
    {
        thumb = null;
        sprite = ReusableActions.GetSprite?.Invoke(_entity);
        if (sprite is null)
            return false;

        const string fileName = "sprite.png";
        thumb = new FileAttachment(sprite, fileName);
        WithThumbnailUrl($"attachment://{fileName}");
        return true;
    }
}
