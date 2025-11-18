using PKHeX.Core;
using SysBot.Base;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutorBase(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> Config)
    : SwitchRoutineExecutor<PokeBotState>(Config)
{
    public static readonly System.Version BotbaseVersion = new(2, 4);

    public LanguageID GameLang { get; private set; }
    public GameVersion Version { get; private set; }
    public EntityContext Context { get; private set; }
    public string InGameName { get; private set; } = "SysBot.NET";

    public static readonly TrackedUserLog PreviousUsers = new();
    public static readonly TrackedUserLog PreviousUsersDistribution = new();

    public override string GetSummary()
    {
        var current = Config.CurrentRoutineType;
        var initial = Config.InitialRoutine;
        if (current == initial)
            return $"{Connection.Label} - {initial}";
        return $"{Connection.Label} - {initial} ({current})";
    }

    protected void InitSaveData(SaveFile sav)
    {
        GameLang = (LanguageID)sav.Language;
        Version = sav.Version;
        InGameName = sav.OT;
        Context = sav.Context;
        Connection.Label = $"{InGameName}-{sav.DisplayTID:000000}";
        Log($"{Connection.Name} identified as {Connection.Label}, using {GameLang}.");
    }

    protected bool IsValidTrainerData()
    {
        if (InGameName.Length == 0)
            return false;
        if (GameLang is 0 or LanguageID.UNUSED_6)
            return false;

        if (!Version.IsValidSavedVersion())
            return false;

        if (Version.GetContext() != Context)
            return false;

        return GameLang <= (Context switch
        {
            EntityContext.Gen9a => LanguageID.SpanishL,
            _ => LanguageID.ChineseT,
        });
    }

    public override void SoftStop() => Config.Pause();

    public Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
        Click(b, Util.Rand.Next(delayMin, delayMax), token);

    public Task SetStick(SwitchStick stick, short x, short y, int delayMin, int delayMax, CancellationToken token) =>
        SetStick(stick, x, y, Util.Rand.Next(delayMin, delayMax), token);
}
