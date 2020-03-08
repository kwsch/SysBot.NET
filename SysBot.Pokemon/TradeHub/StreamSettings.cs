using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public class StreamSettings
    {
        private const string Operation = nameof(Operation);

        [Category(Operation), Description("Generate trade start details.")]
        public bool CreateTradeStart { get; set; } = true;

        // On Deck

        [Category(Operation), Description("Generate a list of People currently on-deck #2.")]
        public bool CreateOnDeck { get; set; } = true;

        [Category(Operation), Description("Amount of users to show in the on-deck list.")]
        public int OnDeckTake { get; set; } = 5;

        [Category(Operation), Description("Amount of on-deck users to skip at the top. If you want to hide people being processed, set this to your amount of consoles.")]
        public int OnDeckSkip { get; set; } = 0;

        [Category(Operation), Description("Separator to split the on-deck list users.")]
        public string OnDeckSeparator { get; set; } = "\n";

        [Category(Operation), Description("Separator to split the on-deck list users.")]
        public string OnDeckFormat { get; set; } = "(ID {0}) - {3}";

        // On Deck 2

        [Category(Operation), Description("Generate a list of People currently on-deck #2.")]
        public bool CreateOnDeck2 { get; set; } = true;

        [Category(Operation), Description("Amount of users to show in the on-deck #2 list.")]
        public int OnDeckTake2 { get; set; } = 5;

        [Category(Operation), Description("Amount of on-deck #2 users to skip at the top. If you want to hide people being processed, set this to your amount of consoles.")]
        public int OnDeckSkip2 { get; set; } = 0;

        [Category(Operation), Description("Separator to split the on-deck #2 list users.")]
        public string OnDeckSeparator2 { get; set; } = "\n";

        [Category(Operation), Description("Separator to split the on-deck #2 list users.")]
        public string OnDeckFormat2 { get; set; } = "(ID {0}) - {3}";

        // User List

        [Category(Operation), Description("Generate a list of People currently being traded.")]
        public bool CreateUserList { get; set; } = true;

        [Category(Operation), Description("Amount of users to show in the list.")]
        public int UserListTake { get; set; } = -1;

        [Category(Operation), Description("Amount of users to skip at the top. If you want to hide people being processed, set this to your amount of consoles.")]
        public int UserListSkip { get; set; } = 0;

        [Category(Operation), Description("Separator to split the list users.")]
        public string UserListSeparator { get; set; } = ", ";

        [Category(Operation), Description("Separator to split the list users.")]
        public string UserListFormat { get; set; } = "(ID {0}) - {3}";

        // TradeCodeBlock

        [Category(Operation), Description("Copies the file .")]
        public bool CopyImageFile { get; set; } = true;

        [Category(Operation), Description("Separator to split the on-deck list users.")]
        public string TradeBlockFile { get; set; } = string.Empty;

        [Category(Operation), Description("Separator to split the on-deck list users.")]
        public string TradeBlockFormat { get; set; } = "block_{0}.png"; // {0} gets replaced with the local IP address

        // Waited Time

        [Category(Operation), Description("Create a file listing the amount of time the most recently dequeued user has waited.")]
        public bool CreateWaitedTime { get; set; } = true;

        [Category(Operation), Description("Format to display the Waited Time.")]
        public string WaitedTimeFormat { get; set; } = @"hh\:mm\:ss";

        // Users in Queue

        [Category(Operation), Description("Create a file indicating the count of users in the queue.")]
        public bool CreateUsersInQueue { get; set; } = true;

        [Category(Operation), Description("Format to display the Users in Queue.")]
        public string UsersInQueueFormat { get; set; } = "Users in Queue: {0}";

        public void StartTrade(PokeTradeBot b, PokeTradeDetail<PK8> detail, PokeTradeHub<PK8> hub)
        {
            try
            {
                if (CreateTradeStart)
                    GenerateBotConnection(b, detail);
                if (CreateWaitedTime)
                    GenerateWaitedTime(detail.Time);
                if (CreateUsersInQueue)
                    GenerateUsersInQueue(hub.Queues.Info.Count);
                if (CreateOnDeck)
                    GenerateOnDeck(hub);
                if (CreateOnDeck2)
                    GenerateOnDeck2(hub);
                if (CreateUserList)
                    GenerateUserList(hub);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.Message, "Stream");
            }
        }

        private void GenerateUsersInQueue(int count)
        {
            var value = string.Format(UserListFormat, count);
            File.WriteAllText("queuecount.txt", value);
        }

        private void GenerateWaitedTime(DateTime time)
        {
            var now = DateTime.Now;
            var difference = now - time;
            var value = difference.ToString(WaitedTimeFormat);
            File.WriteAllText("waited.txt", value);
        }

        public void StartEnterCode(PokeTradeBot b)
        {
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
                LogUtil.LogError(e.Message, "Stream");
            }
        }

        private static readonly byte[] BlackPixel = // 1x1 black pixel
        {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };

        public void EndEnterCode(PokeTradeBot b)
        {
            try
            {
                var file = GetBlockFileName(b);
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception e)
            {
                LogUtil.LogError(e.Message, "Stream");
            }
        }

        private string GetBlockFileName(PokeTradeBot b) => string.Format(TradeBlockFormat, b.Connection.IP);

        private static void GenerateBotConnection(PokeTradeBot b, PokeTradeDetail<PK8> detail)
        {
            var file = b.Connection.IP;
            var name = $"(ID {detail.ID}) {detail.Trainer.TrainerName}";
            File.WriteAllText($"{file}.txt", name);
        }

        private void GenerateOnDeck(PokeTradeHub<PK8> hub)
        {
            var ondeck = hub.Queues.Info.GetUserList(OnDeckFormat);
            ondeck = ondeck.Skip(OnDeckSkip).Take(OnDeckTake); // filter down
            File.WriteAllText("ondeck.txt", string.Join(OnDeckSeparator, ondeck));
        }

        private void GenerateOnDeck2(PokeTradeHub<PK8> hub)
        {
            var ondeck = hub.Queues.Info.GetUserList(OnDeckFormat2);
            ondeck = ondeck.Skip(OnDeckSkip2).Take(OnDeckTake2); // filter down
            File.WriteAllText("ondeck2.txt", string.Join(OnDeckSeparator2, ondeck));
        }

        private void GenerateUserList(PokeTradeHub<PK8> hub)
        {
            var users = hub.Queues.Info.GetUserList(UserListFormat);
            users = users.Skip(UserListSkip);
            if (UserListTake > 0)
                users = users.Take(UserListTake); // filter down
            File.WriteAllText("users.txt", string.Join(UserListSeparator, users));
        }
    }
}