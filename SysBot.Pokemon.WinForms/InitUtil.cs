using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;

namespace SysBot.Pokemon.WinForms;

public static class InitUtil
{
    public static void InitializeStubs(ProgramMode mode, string trainer, LanguageID language)
    {
        Trainer = trainer;
        Language = language;
        var sav = GetFakeSaveFile(mode);
        SetUpSpriteCreator(sav);
    }

    private static string Trainer { get; set; } = "SysBot";
    private static LanguageID Language { get; set; } = LanguageID.English;
    private static SaveFile Get(GameVersion version) => BlankSaveFile.Get(version, Trainer, Language);

    private static SaveFile GetFakeSaveFile(ProgramMode mode) => mode switch
    {
        ProgramMode.SWSH => Get(GameVersion.SW),
        ProgramMode.BDSP => Get(GameVersion.BD),
        ProgramMode.LA   => Get(GameVersion.PLA),
        ProgramMode.SV   => Get(GameVersion.SL),
        ProgramMode.LZA  => Get(GameVersion.ZA),
        _                => throw new System.ArgumentOutOfRangeException(nameof(mode)),
    };

    private static void SetUpSpriteCreator(SaveFile sav)
    {
        SpriteUtil.Initialize(sav);
        StreamSettings.CreateSpriteFile = (pk, fn) =>
        {
            var png = pk.Sprite();
            png.Save(fn);
        };
    }
}
