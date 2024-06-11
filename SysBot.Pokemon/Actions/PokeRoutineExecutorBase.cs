using PKHeX.Core;
using SysBot.Base;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutorBase(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> Config)
    : SwitchRoutineExecutor<PokeBotState>(Config)
{
    public const decimal BotbaseVersion = 2.3m;

    public static readonly TrackedUserLog PreviousUsers = new();

    public static readonly TrackedUserLog PreviousUsersDistribution = new();

    public LanguageID GameLang { get; private set; }

    public string InGameName { get; private set; } = "Shinypkm.com";

    public GameVersion Version { get; private set; }

    public Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
        Click(b, Util.Rand.Next(delayMin, delayMax), token);

    public override string GetSummary()
    {
        var current = Config.CurrentRoutineType;
        var initial = Config.InitialRoutine;
        if (current == initial)
            return $"{Connection.Label} - {initial}";
        return $"{Connection.Label} - {initial} ({current})";
    }

    public Task SetStick(SwitchStick stick, short x, short y, int delayMin, int delayMax, CancellationToken token) =>
        SetStick(stick, x, y, Util.Rand.Next(delayMin, delayMax), token);

    public override void SoftStop() => Config.Pause();

    protected void InitSaveData(SaveFile sav)
    {
        GameLang = (LanguageID)sav.Language;
        Version = sav.Version;
        InGameName = sav.OT;
        Connection.Label = $"{InGameName}-{sav.DisplayTID:000000}";
        Log($"{Connection.Name} identified as {Connection.Label}, using {GameLang}.");
    }

    protected bool IsValidTrainerData() => GameLang is > 0 and <= LanguageID.ChineseT && InGameName.Length > 0 && Version > 0;
}
