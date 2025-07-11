using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static SysBot.Base.SwitchButton;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Pokemon.Helpers;
using System.Xml;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutor<T>(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> Config)
    : PokeRoutineExecutorBase(Config)
    where T : PKM, new()
{
    public readonly BatchTradeTracker<T> _batchTracker = new();

    private const ulong dmntID = 0x010000000000000d;
    // Check if either Tesla or dmnt are active if the sanity check for Trainer Data fails, as these are common culprits.
    private const ulong ovlloaderID = 0x420000000007e51a;

    public static void DumpPokemon(string folder, string subfolder, T pk)
    {
        if (!Directory.Exists(folder))
            return;
        var dir = Path.Combine(folder, subfolder);
        Directory.CreateDirectory(dir);
        var fn = Path.Combine(dir, PathUtil.CleanFileName(pk.FileName));
        File.WriteAllBytes(fn, pk.DecryptedPartyData);
        LogUtil.LogInfo($"Saved file: {fn}", "Dump");
    }

    public static void LogSuccessfulTrades(PokeTradeDetail<T> poke, ulong TrainerNID, string TrainerName)
    {
        // All users who traded, tracked by whether it was a targeted trade or distribution.
        if (poke.Type == PokeTradeType.Random)
            PreviousUsersDistribution.TryRegister(TrainerNID, TrainerName);
        else
            PreviousUsers.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);
    }

    public async Task CheckForRAMShiftingApps(CancellationToken token)
    {
        Log("Trainer data is not valid.");

        bool found = false;
        var msg = "";
        if (await SwitchConnection.IsProgramRunning(ovlloaderID, token).ConfigureAwait(false))
        {
            msg += "Found Tesla Menu";
            found = true;
        }

        if (await SwitchConnection.IsProgramRunning(dmntID, token).ConfigureAwait(false))
        {
            if (found)
                msg += " and ";
            msg += "dmnt (cheat codes?)";
            found = true;
        }
        if (found)
        {
            msg += ".";
            Log(msg);
            Log("Please remove interfering applications and reboot the Switch.");
        }
    }

    public abstract Task<T> ReadBoxPokemon(int box, int slot, CancellationToken token);
    public abstract Task<T> ReadPokemon(ulong offset, CancellationToken token);
    public abstract Task<T> ReadPokemon(ulong offset, int size, CancellationToken token);
    public abstract Task<T> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token);
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

    public async Task<bool> TryReconnect(int attempts, int extraDelay, SwitchProtocol protocol, CancellationToken token)
    {
        // USB can have several reasons for connection loss, some of which is not recoverable (power loss, sleep).
        // Only deal with Wi-Fi for now.
        if (protocol is SwitchProtocol.WiFi)
        {
            // If ReconnectAttempts is set to -1, this should allow it to reconnect (essentially) indefinitely.
            for (int i = 0; i < (uint)attempts; i++)
            {
                LogUtil.LogInfo($"Trying to reconnect... ({i + 1})", Connection.Label);
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
            msg += $" version is not supported. Expected version {BotbaseVersion} or greater, and current version is {version}. Please download the latest version from: ";
            if (protocol is SwitchProtocol.WiFi)
                msg += "https://github.com/olliz0r/sys-botbase/releases/latest";
            else
                msg += "https://github.com/Koi-3088/usb-botbase/releases/latest";
            throw new Exception(msg);
        }
    }

    protected async Task<PokeTradeResult> CheckPartnerReputation(
        PokeRoutineExecutor<T> bot,
        PokeTradeDetail<T> poke,
        ulong TrainerNID,
        string TrainerName,
        TradeAbuseSettings AbuseSettings,
        CancellationToken token)
    {
        bool quit = false;
        var user = poke.Trainer;
        var isDistribution = poke.Type == PokeTradeType.Random;
        var useridmsg = isDistribution ? "" : $" (NID: {TrainerNID})";
        var list = isDistribution ? PreviousUsersDistribution : PreviousUsers;

        // Check if the NID is in the banned list
        var entry = AbuseSettings.BannedIDs.List.Find(z => z.ID == TrainerNID);
        if (entry != null)
        {
            // Block the user if the setting is enabled
            if (AbuseSettings.BlockDetectedBannedUser && bot is PokeRoutineExecutor8SWSH)
                await BlockUser(token).ConfigureAwait(false);

            var msg = $"{user.TrainerName}{useridmsg} is a banned user, and was encountered in-game using OT: {TrainerName}.";
            if (!string.IsNullOrWhiteSpace(entry.Comment))
                msg += $"\nUser was banned for: {entry.Comment}";
            if (!string.IsNullOrWhiteSpace(AbuseSettings.BannedIDMatchEchoMention))
                msg = $"{AbuseSettings.BannedIDMatchEchoMention} {msg}";

            EchoUtil.EchoAbuseMessage(msg);
            return PokeTradeResult.SuspiciousActivity;
        }

        // Check previous trades with this NID
        var previous = list.TryGetPreviousNID(TrainerNID);
        if (previous != null)
        {
            var delta = DateTime.Now - previous.Time; // Time since last trade
            Log($"Last traded with NID: {TrainerNID} {delta.TotalMinutes:F1} minutes ago (OT: {TrainerName}).");

            // Check if the same NID is using a different Discord account
            if (previous.RemoteID != user.ID && user.ID != 0)
            {
                var abuseExpiration = TimeSpan.FromMinutes(AbuseSettings.TradeAbuseExpiration);
                if (delta < abuseExpiration)
                {
                    var msg = AbuseSettings.EchoNintendoOnlineIDMulti
                        ? $"Detected same NID: {TrainerNID} using different Discord accounts.\n"
                        : "Detected same NID using different Discord accounts.\n";

                    msg += $"Previous: {previous.Name} (Discord ID: {previous.RemoteID})\n" +
                           $"Current: {user.TrainerName} (Discord ID: {user.ID})";

                    if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiAbuseEchoMention))
                        msg = $"{AbuseSettings.MultiAbuseEchoMention} {msg}";

                    EchoUtil.EchoAbuseMessage(msg);

                    if (AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                    {
                        if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                        {
                            await BlockUser(token).ConfigureAwait(false);
                            if (AbuseSettings.BanIDWhenBlockingUser)
                            {
                                AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "Multiple Discord accounts") });
                                Log($"Added {TrainerNID} to the BannedIDs list for using multiple Discord accounts.");
                            }
                        }
                        quit = true;
                    }
                }
            }

            // Cooldown check
            var cd = AbuseSettings.TradeCooldown;
            if (cd != 0 && delta < TimeSpan.FromMinutes(cd))
            {
                var wait = TimeSpan.FromMinutes(cd) - delta;
                poke.Notifier.SendNotification(bot, poke, $"You are still on trade cooldown, and cannot trade for another {wait.TotalMinutes:F1} minute(s).");

                var msg = AbuseSettings.EchoNintendoOnlineIDCooldown
                    ? $"Found NID: {TrainerNID} (OT: {TrainerName}) ignoring the {cd} minute trade cooldown. Last encountered {delta.TotalMinutes:F1} minutes ago."
                    : $"Found user (OT: {TrainerName}) ignoring the {cd} minute trade cooldown. Last encountered {delta.TotalMinutes:F1} minutes ago.";

                if (!string.IsNullOrWhiteSpace(AbuseSettings.CooldownAbuseEchoMention))
                    msg = $"{AbuseSettings.CooldownAbuseEchoMention} {msg}";

                EchoUtil.EchoAbuseMessage(msg);
                return PokeTradeResult.SuspiciousActivity;
            }
        }

        // Check for users sending to multiple in-game accounts (non-distribution trades)
        if (!isDistribution && user.ID != 0)
        {
            var previous_remote = list.TryGetPreviousRemoteID(user.ID);
            if (previous_remote != null && previous_remote.NetworkID != TrainerNID)
            {
                var delta = DateTime.Now - previous_remote.Time;
                var abuseExpiration = TimeSpan.FromMinutes(AbuseSettings.TradeAbuseExpiration);

                if (delta < abuseExpiration)
                {
                    var msg = AbuseSettings.EchoNintendoOnlineIDMultiRecipients
                        ? $"Detected Discord ID: {user.ID} trading with different NIDs.\n" +
                          $"Previous NID: {previous_remote.NetworkID} (OT: {previous_remote.Name})\n" +
                          $"Current NID: {TrainerNID} (OT: {TrainerName})"
                        : $"Detected Discord ID: {user.ID} trading with different in-game accounts.\n" +
                          $"Previous OT: {previous_remote.Name}\n" +
                          $"Current OT: {TrainerName}";

                    if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiRecipientEchoMention))
                        msg = $"{AbuseSettings.MultiRecipientEchoMention} {msg}";

                    EchoUtil.EchoAbuseMessage(msg);

                    if (AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                    {
                        if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                        {
                            await BlockUser(token).ConfigureAwait(false);
                            if (AbuseSettings.BanIDWhenBlockingUser)
                            {
                                AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "Trading with multiple NIDs") });
                                Log($"Added {TrainerNID} to the BannedIDs list for trading with multiple NIDs.");
                            }
                        }
                        quit = true;
                    }
                }
            }
        }
        list.TryRegister(TrainerNID, TrainerName, user.ID);

        if (quit)
            return PokeTradeResult.SuspiciousActivity;

        return PokeTradeResult.Success;
    }

    protected async Task<(bool, ulong)> ValidatePointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        var solved = await SwitchConnection.PointerAll(jumps, token).ConfigureAwait(false);
        return (solved != 0, solved);
    }

    private static RemoteControlAccess GetReference(string name, ulong id, string comment) => new()
    {
        ID = id,
        Name = name,
        Comment = $"Added automatically on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
    };

    // Blocks a user from the box during in-game trades (SWSH).
    private async Task BlockUser(CancellationToken token)
    {
        Log("Blocking user in-game...");
        await PressAndHold(RSTICK, 0_750, 0, token).ConfigureAwait(false);
        await Click(DUP, 0_300, token).ConfigureAwait(false);
        await Click(A, 1_300, token).ConfigureAwait(false);
        await Click(A, 1_300, token).ConfigureAwait(false);
        await Click(DUP, 0_300, token).ConfigureAwait(false);
        await Click(A, 1_100, token).ConfigureAwait(false);
        await Click(A, 1_100, token).ConfigureAwait(false);
    }
}
