using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutor<T>(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> Config)
    : PokeRoutineExecutorBase(Config)
    where T : PKM, new()
{
    public abstract Task<T> ReadPokemon(ulong offset, CancellationToken token);
    public abstract Task<T> ReadPokemon(ulong offset, int size, CancellationToken token);
    public abstract Task<T> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token);
    public abstract Task<T> ReadBoxPokemon(int box, int slot, CancellationToken token);

    public async Task<T?> ReadUntilPresent(ulong offset, int waitms, int waitInterval, int size, CancellationToken token)
    {
        int msWaited = 0;
        while (msWaited < waitms)
        {
            var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
            if (pk.Species != 0 && pk.ChecksumValid)
                return pk;
            await Task.Delay(waitInterval, token).ConfigureAwait(false);
            msWaited += waitInterval;
        }
        return null;
    }

    public async Task<T?> ReadUntilPresentPointer(IReadOnlyList<long> jumps, int waitms, int waitInterval, int size, CancellationToken token)
    {
        int msWaited = 0;
        while (msWaited < waitms)
        {
            var pk = await ReadPokemonPointer(jumps, size, token).ConfigureAwait(false);
            if (pk.Species != 0 && pk.ChecksumValid)
                return pk;
            await Task.Delay(waitInterval, token).ConfigureAwait(false);
            msWaited += waitInterval;
        }
        return null;
    }

    protected async Task<(bool, ulong)> ValidatePointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        var solved = await SwitchConnection.PointerAll(jumps, token).ConfigureAwait(false);
        return (solved != 0, solved);
    }

    public static void DumpPokemon(string folder, string subfolder, T pk)
    {
        if (!Directory.Exists(folder))
            return;
        var dir = Path.Combine(folder, subfolder);
        Directory.CreateDirectory(dir);
        var fn = Path.Combine(dir, Util.CleanFileName(pk.FileName));
        File.WriteAllBytes(fn, pk.DecryptedPartyData);
        LogUtil.LogInfo($"Gespeicherte Datei: {fn}", "Dump");
    }

    public async Task<bool> TryReconnect(int attempts, int extraDelay, SwitchProtocol protocol, CancellationToken token)
    {
        // USB can have several reasons for connection loss, some of which is not recoverable (power loss, sleep).
        // Only deal with Wi-Fi for now.
        if (protocol is SwitchProtocol.WiFi)
        {
            // If ReconnectAttempts is set to -1, this should allow it to reconnect (essentially) indefinitely.
            for (int i = 0; i < (uint)attempts; i++)
            {
                LogUtil.LogInfo($"Ich versuche, wieder eine Verbindung herzustellen... ({i + 1})", Connection.Label);
                Connection.Reset();
                if (Connection.Connected)
                    break;

                await Task.Delay(30_000 + extraDelay, token).ConfigureAwait(false);
            }
        }
        return Connection.Connected;
    }

    public async Task VerifyBotbaseVersion(CancellationToken token)
    {
        var data = await SwitchConnection.GetBotbaseVersion(token).ConfigureAwait(false);
        var version = decimal.TryParse(data, CultureInfo.InvariantCulture, out var v) ? v : 0;
        if (version < BotbaseVersion)
        {
            var protocol = Config.Connection.Protocol;
            var msg = protocol is SwitchProtocol.WiFi ? "sys-botbase" : "usb-botbase";
            msg += $" Version wird nicht unterstützt. Erwartete Version {BotbaseVersion} oder höher, und die aktuelle Version ist {version}. Bitte laden Sie die neueste Version herunter: ";
            if (protocol is SwitchProtocol.WiFi)
                msg += "https://github.com/olliz0r/sys-botbase/releases/latest";
            else
                msg += "https://github.com/Koi-3088/usb-botbase/releases/latest";
            throw new Exception(msg);
        }
    }

    // Check if either Tesla or dmnt are active if the sanity check for Trainer Data fails, as these are common culprits.
    private const ulong ovlloaderID = 0x420000000007e51a; // Tesla Menu
    private const ulong dmntID = 0x010000000000000d;      // dmnt used for cheats

    public async Task CheckForRAMShiftingApps(CancellationToken token)
    {
        Log("Die Trainerdaten sind ungültig.");

        bool found = false;
        var msg = "";
        if (await SwitchConnection.IsProgramRunning(ovlloaderID, token).ConfigureAwait(false))
        {
            msg += "Tesla-Menü gefunden";
            found = true;
        }

        if (await SwitchConnection.IsProgramRunning(dmntID, token).ConfigureAwait(false))
        {
            if (found)
                msg += " und ";
            msg += "dmnt (Cheat-Codes?)";
            found = true;
        }
        if (found)
        {
            msg += ".";
            Log(msg);
            Log("Bitte entfernen Sie störende Anwendung(en) und starten Sie die Switch neu.");
        }
    }

    protected async Task<PokeTradeResult> CheckPartnerReputation(PokeRoutineExecutor<T> bot, PokeTradeDetail<T> poke, ulong TrainerNID, string TrainerName,
        TradeAbuseSettings AbuseSettings, CancellationToken token)
    {
        bool quit = false;
        var user = poke.Trainer;
        var isDistribution = poke.Type == PokeTradeType.Random;
        var useridmsg = isDistribution ? "" : $" ({user.ID})";
        var list = isDistribution ? PreviousUsersDistribution : PreviousUsers;

        // Matches to a list of banned NIDs, in case the user ever manages to enter a trade.
        var entry = AbuseSettings.BannedIDs.List.Find(z => z.ID == TrainerNID);
        if (entry != null)
        {
            if (AbuseSettings.BlockDetectedBannedUser && bot is PokeRoutineExecutor8SWSH)
                await BlockUser(token).ConfigureAwait(false);

            var msg = $"{user.TrainerName}{useridmsg} ist ein gesperrter Benutzer, der im Spiel mit folgendem OT angetroffen wurde: {TrainerName}.";
            if (!string.IsNullOrWhiteSpace(entry.Comment))
                msg += $"\nDer Benutzer wurde gesperrt für: {entry.Comment}";
            if (!string.IsNullOrWhiteSpace(AbuseSettings.BannedIDMatchEchoMention))
                msg = $"{AbuseSettings.BannedIDMatchEchoMention} {msg}";
            EchoUtil.Echo(msg);
            return PokeTradeResult.SuspiciousActivity;
        }

        // Check within the trade type (distribution or non-Distribution).
        var previous = list.TryGetPreviousNID(TrainerNID);
        if (previous != null)
        {
            var delta = DateTime.Now - previous.Time; // Time that has passed since last trade.
            Log($"Zuletzt gehandelt mit {user.TrainerName} {delta.TotalMinutes:F1} Minuten zuvor (OT: {TrainerName}).");

            // Allows setting a cooldown for repeat trades. If the same user is encountered within the cooldown period for the same trade type, the user is warned and the trade will be ignored.
            var cd = AbuseSettings.TradeCooldown;     // Time they must wait before trading again.
            if (cd != 0 && TimeSpan.FromMinutes(cd) > delta)
            {
                var wait = TimeSpan.FromMinutes(cd) - delta;
                poke.Notifier.SendNotification(bot, poke, $"Sie befinden sich immer noch in der Abklingphase des Handels und können für weitere {wait.TotalMinutes:F1} Minute(n) nicht handeln.");
                var msg = $"{user.TrainerName}{useridmsg} gefunden, der die Abklingzeit für den Handel von {cd} Minuten ignoriert. Wurde zuletzt vor {delta.TotalMinutes:F1} Minuten gefunden.";
                if (AbuseSettings.EchoNintendoOnlineIDCooldown)
                    msg += $"\nID: {TrainerNID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.CooldownAbuseEchoMention))
                    msg = $"{AbuseSettings.CooldownAbuseEchoMention} {msg}";
                EchoUtil.Echo(msg);
                return PokeTradeResult.SuspiciousActivity;
            }

            // For non-Distribution trades, flag users using multiple Discord/Twitch accounts to send to the same in-game player within a time limit.
            // This is usually to evade a ban or a trade cooldown.
            if (!isDistribution && previous.NetworkID == TrainerNID && previous.RemoteID != user.ID)
            {
                if (delta < TimeSpan.FromMinutes(AbuseSettings.TradeAbuseExpiration) && AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                {
                    if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                    {
                        await BlockUser(token).ConfigureAwait(false);
                        if (AbuseSettings.BanIDWhenBlockingUser || bot is not PokeRoutineExecutor8SWSH) // Only ban ID if blocking in SWSH, always in other games.
                        {
                            AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "Spielinterne Sperre für mehrere Konten") });
                            Log($"{TrainerNID} zur Liste der gesperrten IDs hinzugefügt.");
                        }
                    }
                    quit = true;
                }

                var msg = $"{user.TrainerName}{useridmsg} mit mehreren Konten gefunden.\nVormals gehandelt mit {previous.Name} ({previous.RemoteID}) vor {delta.TotalMinutes:F1} Minuten auf OT: {TrainerName}.";
                if (AbuseSettings.EchoNintendoOnlineIDMulti)
                    msg += $"\nID: {TrainerNID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiAbuseEchoMention))
                    msg = $"{AbuseSettings.MultiAbuseEchoMention} {msg}";
                EchoUtil.Echo(msg);
            }
        }

        // For non-Distribution trades, we can optionally flag users sending to multiple in-game players.
        // Can trigger if the user gets sniped, but can also catch abusers sending to many people.
        if (!isDistribution)
        {
            var previous_remote = PreviousUsers.TryGetPreviousRemoteID(poke.Trainer.ID);
            if (previous_remote != null && previous_remote.Name != TrainerName)
            {
                if (AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                {
                    if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                    {
                        await BlockUser(token).ConfigureAwait(false);
                        if (AbuseSettings.BanIDWhenBlockingUser || bot is not PokeRoutineExecutor8SWSH) // Only ban ID if blocking in SWSH, always in other games.
                        {
                            AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "hat eine Spielinterne Sperre für das Versenden an mehrere Spieler im Spiel") });
                            Log($"{TrainerNID} zur Liste der gesperrten IDs hinzugefügt.");
                        }
                    }
                    quit = true;
                }

                var msg = $"Es wurde festgestellt, dass {user.TrainerName}{useridmsg} an mehrere Spieler im Spiel sendet. Vorheriger OT: {previous_remote.Name}, Aktueller OT: {TrainerName}";
                if (AbuseSettings.EchoNintendoOnlineIDMultiRecipients)
                    msg += $"\nID: {TrainerNID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiRecipientEchoMention))
                    msg = $"{AbuseSettings.MultiRecipientEchoMention} {msg}";
                EchoUtil.Echo(msg);
            }
        }

        if (quit)
            return PokeTradeResult.SuspiciousActivity;

        return PokeTradeResult.Success;
    }

    public static void LogSuccessfulTrades(PokeTradeDetail<T> poke, ulong TrainerNID, string TrainerName)
    {
        // All users who traded, tracked by whether it was a targeted trade or distribution.
        if (poke.Type == PokeTradeType.Random)
            PreviousUsersDistribution.TryRegister(TrainerNID, TrainerName);
        else
            PreviousUsers.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);
    }

    private static RemoteControlAccess GetReference(string name, ulong id, string comment) => new()
    {
        ID = id,
        Name = name,
        Comment = $"Automatisch hinzugefügt am {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
    };

    // Blocks a user from the box during in-game trades (SWSH).
    private async Task BlockUser(CancellationToken token)
    {
        Log("Benutzer im Spiel blockieren...");
        await PressAndHold(RSTICK, 0_750, 0, token).ConfigureAwait(false);
        await Click(DUP, 0_300, token).ConfigureAwait(false);
        await Click(A, 1_300, token).ConfigureAwait(false);
        await Click(A, 1_300, token).ConfigureAwait(false);
        await Click(DUP, 0_300, token).ConfigureAwait(false);
        await Click(A, 1_100, token).ConfigureAwait(false);
        await Click(A, 1_100, token).ConfigureAwait(false);
    }
}
