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

    public override string ToString() => "Stream-Einstellungen";
    public static Action<PKM, string>? CreateSpriteFile { get; set; }

    [Category(Operation), Description("Erzeugen von Stream-Assets; durch Ausschalten wird die Erzeugung von Assets verhindert.")]
    public bool CreateAssets { get; set; }

    [Category(Operation), Description("Generieren Sie Details zum Handelsbeginn, die angeben, mit wem der Bot handelt.")]
    public bool CreateTradeStart { get; set; } = true;

    [Category(Operation), Description("Generieren Sie Details zum Handelsbeginn, die angeben, womit der Bot handelt.")]
    public bool CreateTradeStartSprite { get; set; } = true;

    [Category(Operation), Description("Format für die Anzeige der Now Trading Details. {0} = ID, {1} = Benutzer")]
    public string TrainerTradeStart { get; set; } = "(ID {0}) {1}";

    // On Deck

    [Category(Operation), Description("Erzeugen Sie eine Liste der Personen, die derzeit an Deck sind.")]
    public bool CreateOnDeck { get; set; } = true;

    [Category(Operation), Description("Anzahl der Benutzer, die in der Deckliste angezeigt werden sollen.")]
    public int OnDeckTake { get; set; } = 5;

    [Category(Operation), Description("Anzahl der Benutzer, die an der Spitze übersprungen werden sollen. Wenn Sie Personen, die bearbeitet werden, ausblenden möchten, setzen Sie diesen Wert auf die Anzahl der Konsolen.")]
    public int OnDeckSkip { get; set; }

    [Category(Operation), Description("Trennzeichen zur Aufteilung der Benutzer der Deckliste.")]
    public string OnDeckSeparator { get; set; } = "\n";

    [Category(Operation), Description("Format zur Anzeige der Benutzer auf der Deckliste. {0} = ID, {3} = Benutzer")]
    public string OnDeckFormat { get; set; } = "(ID {0}) - {3}";

    // On Deck 2

    [Category(Operation), Description("Erstellen Sie eine Liste der Personen, die derzeit auf dem Deck #2 sind.")]
    public bool CreateOnDeck2 { get; set; } = true;

    [Category(Operation), Description("Anzahl der Benutzer, die in der Liste \"on-deck #2\" angezeigt werden sollen.")]
    public int OnDeckTake2 { get; set; } = 5;

    [Category(Operation), Description("Anzahl der on-deck #2-Benutzer, die an der Spitze übersprungen werden sollen. Wenn Sie Personen, die bearbeitet werden, ausblenden möchten, setzen Sie diesen Wert auf die Anzahl der Konsolen.")]
    public int OnDeckSkip2 { get; set; }

    [Category(Operation), Description("Trennzeichen zur Aufteilung der Benutzer der #2-Liste.")]
    public string OnDeckSeparator2 { get; set; } = "\n";

    [Category(Operation), Description("Format zur Anzeige der Benutzer auf der #2-Liste. {0} = ID, {3} = Benutzer")]
    public string OnDeckFormat2 { get; set; } = "(ID {0}) - {3}";

    // User List

    [Category(Operation), Description("Erstellen Sie eine Liste der Personen, die derzeit gehandelt werden.")]
    public bool CreateUserList { get; set; } = true;

    [Category(Operation), Description("Anzahl der Benutzer, die in der Liste angezeigt werden sollen.")]
    public int UserListTake { get; set; } = -1;

    [Category(Operation), Description("Anzahl der Benutzer, die am Anfang übersprungen werden sollen. Wenn Sie Personen, die bearbeitet werden, ausblenden möchten, setzen Sie diesen Wert auf die Anzahl der Konsolen.")]
    public int UserListSkip { get; set; }

    [Category(Operation), Description("Trennzeichen zum Aufteilen der Liste Benutzer.")]
    public string UserListSeparator { get; set; } = ", ";

    [Category(Operation), Description("Format für die Anzeige der Benutzerliste. {0} = ID, {3} = Benutzer")]
    public string UserListFormat { get; set; } = "(ID {0}) - {3}";

    // TradeCodeBlock

    [Category(Operation), Description("Kopiert die TradeBlockFile, wenn sie existiert, andernfalls wird stattdessen ein Platzhalterbild kopiert.")]
    public bool CopyImageFile { get; set; } = true;

    [Category(Operation), Description("Quelldateiname des Bildes, das kopiert werden soll, wenn ein Handelscode eingegeben wird. Wenn leer gelassen, wird ein Platzhalterbild erstellt.")]
    public string TradeBlockFile { get; set; } = string.Empty;

    [Category(Operation), Description("Name der Zieldatei für das Linkcode-Sperrbild. {0} wird durch die lokale IP-Adresse ersetzt.")]
    public string TradeBlockFormat { get; set; } = "block_{0}.png";

    // Waited Time

    [Category(Operation), Description("Erstellen Sie eine Datei, in der die Wartezeit des Benutzers, der zuletzt aus der Warteschlange genommen wurde, aufgeführt ist.")]
    public bool CreateWaitedTime { get; set; } = true;

    [Category(Operation), Description("Format zur Anzeige der Wartezeit für den zuletzt aus der Warteschlange entfernten Benutzer.")]
    public string WaitedTimeFormat { get; set; } = @"hh\:mm\:ss";

    // Estimated Time

    [Category(Operation), Description("Erstellen Sie eine Datei, in der die geschätzte Wartezeit eines Benutzers aufgeführt ist, wenn er sich in die Warteschlange einreiht.")]
    public bool CreateEstimatedTime { get; set; } = true;

    [Category(Operation), Description("Format, um die geschätzte Wartezeit anzuzeigen.")]
    public string EstimatedTimeFormat { get; set; } = "Geschätzte Zeit: {0:F1} minutes";

    [Category(Operation), Description("Format zur Anzeige des geschätzten Wartezeitstempels.")]
    public string EstimatedFulfillmentFormat { get; set; } = @"hh\:mm\:ss";

    // Users in Queue

    [Category(Operation), Description("Erstellen Sie eine Datei, die die Anzahl der Benutzer in der Warteschlange angibt.")]
    public bool CreateUsersInQueue { get; set; } = true;

    [Category(Operation), Description("Format zur Anzeige der Benutzer in der Warteschlange. {0} = Anzahl")]
    public string UsersInQueueFormat { get; set; } = "Benutzer in der Warteschlange: {0}";

    // Completed Trades

    [Category(Operation), Description("Erstellen Sie eine Datei, die die Anzahl der abgeschlossenen Geschäfte angibt, wenn ein neues Geschäft beginnt.")]
    public bool CreateCompletedTrades { get; set; } = true;

    [Category(Operation), Description("Format zur Anzeige der abgeschlossenen Geschäfte. {0} = Anzahl")]
    public string CompletedTradesFormat { get; set; } = "Abgeschlossene Trades: {0}";

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
                File.WriteAllText("estimatedTime.txt", "Geschätzte Zeit: 0 minutes");
                File.WriteAllText("estimatedTimestamp.txt", "");
            }
            if (CreateOnDeck)
                File.WriteAllText("ondeck.txt", "Waiting...");
            if (CreateOnDeck2)
                File.WriteAllText("ondeck2.txt", "Die Warteschlange ist leer!");
            if (CreateUserList)
                File.WriteAllText("users.txt", "None");
            if (CreateUsersInQueue)
                File.WriteAllText("queuecount.txt", "Benutzer in der Warteschlange: 0");
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
