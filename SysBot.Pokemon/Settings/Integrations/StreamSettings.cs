using PKHeX.Core;
using SysBot.Base;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon;

public class StreamSettings
{
    private const string Operation = nameof(Operation);

    public override string ToString() => "Stream Settings";
    public static Action<PKM, string>? CreateSpriteFile { get; set; }

    [Category(Operation), Description("Generate stream assets; turning off will prevent generation of assets.")]
    public bool CreateAssets { get; set; }

    [Category(Operation), Description("Generate trade start details, indicating who the bot is trading with.")]
    public bool CreateTradeStart { get; set; } = true;

    [Category(Operation), Description("Generate trade start details, indicating what the bot is trading.")]
    public bool CreateTradeStartSprite { get; set; } = true;

    [Category(Operation), Description("Format to display the Now Trading details. {0} = ID, {1} = User")]
    public string TrainerTradeStart { get; set; } = "(ID {0}) {1}";

    // On Deck

    [Category(Operation), Description("Generate a list of People currently on-deck.")]
    public bool CreateOnDeck { get; set; } = true;

    [Category(Operation), Description("Number of users to show in the on-deck list.")]
    public int OnDeckTake { get; set; } = 5;

    [Category(Operation), Description("Number of on-deck users to skip at the top. If you want to hide people being processed, set this to your number of consoles.")]
    public int OnDeckSkip { get; set; }

    [Category(Operation), Description("Separator to split the on-deck list users.")]
    public string OnDeckSeparator { get; set; } = "\n";

    [Category(Operation), Description("Format to display the on-deck list users. {0} = ID, {3} = User")]
    public string OnDeckFormat { get; set; } = "(ID {0}) - {3}";

    // On Deck 2

    [Category(Operation), Description("Generate a list of People currently on-deck #2.")]
    public bool CreateOnDeck2 { get; set; } = true;

    [Category(Operation), Description("Number of users to show in the on-deck #2 list.")]
    public int OnDeckTake2 { get; set; } = 5;

    [Category(Operation), Description("Number of on-deck #2 users to skip at the top. If you want to hide people being processed, set this to your number of consoles.")]
    public int OnDeckSkip2 { get; set; }

    [Category(Operation), Description("Separator to split the on-deck #2 list users.")]
    public string OnDeckSeparator2 { get; set; } = "\n";

    [Category(Operation), Description("Format to display the on-deck #2 list users. {0} = ID, {3} = User")]
    public string OnDeckFormat2 { get; set; } = "(ID {0}) - {3}";

    // User List

    [Category(Operation), Description("Generate a list of People currently being traded.")]
    public bool CreateUserList { get; set; } = true;

    [Category(Operation), Description("Number of users to show in the list.")]
    public int UserListTake { get; set; } = -1;

    [Category(Operation), Description("Number of users to skip at the top. If you want to hide people being processed, set this to your number of consoles.")]
    public int UserListSkip { get; set; }

    [Category(Operation), Description("Separator to split the list users.")]
    public string UserListSeparator { get; set; } = ", ";

    [Category(Operation), Description("Format to display the list users. {0} = ID, {3} = User")]
    public string UserListFormat { get; set; } = "(ID {0}) - {3}";

    // TradeCodeBlock

    [Category(Operation), Description("Copies the TradeBlockFile if it exists, otherwise, a placeholder image is copied instead.")]
    public bool CopyImageFile { get; set; } = true;

    [Category(Operation), Description("Source File name of the image to be copied when a trade code is being entered. If left empty, will create a placeholder image.")]
    public string TradeBlockFile { get; set; } = string.Empty;

    [Category(Operation), Description("Destination file name of the Link Code blocking image. {0} gets replaced with the local IP address.")]
    public string TradeBlockFormat { get; set; } = "block_{0}.png";

    // Waited Time

    [Category(Operation), Description("Create a file listing the amount of time the most recently dequeued user has waited.")]
    public bool CreateWaitedTime { get; set; } = true;

    [Category(Operation), Description("Format to display the Waited Time for the most recently dequeued user.")]
    public string WaitedTimeFormat { get; set; } = @"hh\:mm\:ss";

    // Estimated Time

    [Category(Operation), Description("Create a file listing the estimated amount of time a user will have to wait if they joined the queue.")]
    public bool CreateEstimatedTime { get; set; } = true;

    [Category(Operation), Description("Format to display the Estimated Wait Time.")]
    public string EstimatedTimeFormat { get; set; } = "Estimated time: {0:F1} minutes";

    [Category(Operation), Description("Format to display the Estimated Wait Timestamp.")]
    public string EstimatedFulfillmentFormat { get; set; } = @"hh\:mm\:ss";

    // Users in Queue

    [Category(Operation), Description("Create a file indicating the count of users in the queue.")]
    public bool CreateUsersInQueue { get; set; } = true;

    [Category(Operation), Description("Format to display the Users in Queue. {0} = Count")]
    public string UsersInQueueFormat { get; set; } = "Users in Queue: {0}";

    // Completed Trades

    [Category(Operation), Description("Create a file indicating the count of completed trades when a new trade starts.")]
    public bool CreateCompletedTrades { get; set; } = true;

    [Category(Operation), Description("Format to display the Completed Trades. {0} = Count")]
    public string CompletedTradesFormat { get; set; } = "Completed Trades: {0}";

    public void StartTrade<T>(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail, PokeTradeHub<T> hub) where T : PKM, new()
    {
        if (!CreateAssets)
            return;

        try
        {
            if (CreateTradeStart)
                GenerateBotConnection(b, detail);
            if (CreateWaitedTime)
                GenerateWaitedTime(detail.Time);
            if (CreateEstimatedTime)
                GenerateEstimatedTime(hub);
            if (CreateUsersInQueue)
                GenerateUsersInQueue(hub.Queues.Info.Count);
            if (CreateOnDeck)
                GenerateOnDeck(hub);
            if (CreateOnDeck2)
                GenerateOnDeck2(hub);
            if (CreateUserList)
                GenerateUserList(hub);
            if (CreateCompletedTrades)
                GenerateCompletedTrades(hub);
            if (CreateTradeStartSprite)
                GenerateBotSprite(b, detail);
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.Message, nameof(StreamSettings));
        }
    }

    public void IdleAssets(PokeRoutineExecutorBase b)
    {
        if (!CreateAssets)
            return;

        try
        {
            var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (file.Contains(b.Connection.Name))
                    File.Delete(file);
            }

            if (CreateWaitedTime)
                File.WriteAllText("waited.txt", "00:00:00");
            if (CreateEstimatedTime)
            {
                File.WriteAllText("estimatedTime.txt", "Estimated time: 0 minutes");
                File.WriteAllText("estimatedTimestamp.txt", "");
            }
            if (CreateOnDeck)
                File.WriteAllText("ondeck.txt", "Waiting...");
            if (CreateOnDeck2)
                File.WriteAllText("ondeck2.txt", "Queue is empty!");
            if (CreateUserList)
                File.WriteAllText("users.txt", "None");
            if (CreateUsersInQueue)
                File.WriteAllText("queuecount.txt", "Users in Queue: 0");
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.Message, nameof(StreamSettings));
        }
    }

    private void GenerateUsersInQueue(int count)
    {
        var value = string.Format(UsersInQueueFormat, count);
        File.WriteAllText("queuecount.txt", value);
    }

    private void GenerateWaitedTime(DateTime time)
    {
        var now = DateTime.Now;
        var difference = now - time;
        var value = difference.ToString(WaitedTimeFormat);
        File.WriteAllText("waited.txt", value);
    }

    private void GenerateEstimatedTime<T>(PokeTradeHub<T> hub) where T : PKM, new()
    {
        var count = hub.Queues.Info.Count;
        var estimate = hub.Config.Queues.EstimateDelay(count, hub.Bots.Count);

        // Minutes
        var wait = string.Format(EstimatedTimeFormat, estimate);
        File.WriteAllText("estimatedTime.txt", wait);

        // Expected to be fulfilled at this time
        var now = DateTime.Now;
        var difference = now.AddMinutes(estimate);
        var date = difference.ToString(EstimatedFulfillmentFormat);
        File.WriteAllText("estimatedTimestamp.txt", date);
    }

    public void StartEnterCode(PokeRoutineExecutorBase b)
    {
        if (!CreateAssets)
            return;

        try
        {
            var file = GetBlockFileName(b);
            if (CopyImageFile && File.Exists(TradeBlockFile))
                File.Copy(TradeBlockFile, file);
            else
                File.WriteAllBytes(file, BlackPixel);
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.Message, nameof(StreamSettings));
        }
    }

    private static readonly byte[] BlackPixel = // 1x1 black pixel
    [
        0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
        0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
        0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00,
    ];

    public void EndEnterCode(PokeRoutineExecutorBase b)
    {
        try
        {
            var file = GetBlockFileName(b);
            if (File.Exists(file))
                File.Delete(file);
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.Message, nameof(StreamSettings));
        }
    }

    private string GetBlockFileName(PokeRoutineExecutorBase b) => string.Format(TradeBlockFormat, b.Connection.Name);

    private void GenerateBotConnection<T>(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail) where T : PKM, new()
    {
        var file = b.Connection.Name;
        var name = string.Format(TrainerTradeStart, detail.ID, detail.Trainer.TrainerName, (Species)detail.TradeData.Species);
        File.WriteAllText($"{file}.txt", name);
    }

    private static void GenerateBotSprite<T>(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail) where T : PKM, new()
    {
        var func = CreateSpriteFile;
        if (func == null)
            return;
        var file = b.Connection.Name;
        var pk = detail.TradeData;
        func.Invoke(pk, $"sprite_{file}.png");
    }

    private void GenerateOnDeck<T>(PokeTradeHub<T> hub) where T : PKM, new()
    {
        var ondeck = hub.Queues.Info.GetUserList(OnDeckFormat);
        ondeck = ondeck.Skip(OnDeckSkip).Take(OnDeckTake); // filter down
        File.WriteAllText("ondeck.txt", string.Join(OnDeckSeparator, ondeck));
    }

    private void GenerateOnDeck2<T>(PokeTradeHub<T> hub) where T : PKM, new()
    {
        var ondeck = hub.Queues.Info.GetUserList(OnDeckFormat2);
        ondeck = ondeck.Skip(OnDeckSkip2).Take(OnDeckTake2); // filter down
        File.WriteAllText("ondeck2.txt", string.Join(OnDeckSeparator2, ondeck));
    }

    private void GenerateUserList<T>(PokeTradeHub<T> hub) where T : PKM, new()
    {
        var users = hub.Queues.Info.GetUserList(UserListFormat);
        users = users.Skip(UserListSkip);
        if (UserListTake > 0)
            users = users.Take(UserListTake); // filter down
        File.WriteAllText("users.txt", string.Join(UserListSeparator, users));
    }

    private void GenerateCompletedTrades<T>(PokeTradeHub<T> hub) where T : PKM, new()
    {
        var msg = string.Format(CompletedTradesFormat, hub.Config.Trade.CompletedTrades);
        File.WriteAllText("completed.txt", msg);
    }
}
