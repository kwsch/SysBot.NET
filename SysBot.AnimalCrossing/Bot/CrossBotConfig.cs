using System;
using System.Collections.Generic;
using System.Linq;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    public sealed class CrossBotConfig : SwitchBotConfig, IConfigItem
    {
        public bool AcceptingCommands { get; set; } = true;
        public string Name { get; set; } = "CrossBot";
        public string Token { get; set; } = "DISCORD_TOKEN";
        public string Prefix { get; set; } = "$";

        public string RoleCustom { get; set; } = string.Empty;

        public uint Offset { get; set; } = 0xABADD888;
        public bool WrapAllItems { get; set; } = true;
        public ItemWrappingPaper WrappingPaper { get; set; } = ItemWrappingPaper.Black;
        public bool AutoClean { get; set; }

        public List<ulong> Channels { get; set; } = new List<ulong>();
        public List<ulong> Users { get; set; } = new List<ulong>();
        public List<ulong> Sudo { get; set; } = new List<ulong>();

        public bool CanUseCommandUser(ulong authorId) => Users.Count == 0 || Users.Contains(authorId);
        public bool CanUseCommandChannel(ulong channelId) => Channels.Count == 0 || Channels.Contains(channelId);
        public bool CanUseSudo(ulong userId) => Sudo.Contains(userId);

        public bool GetHasRole(string roleName, IEnumerable<string> roles)
        {
            return roleName switch
            {
                nameof(RoleCustom) => roles.Contains(RoleCustom),
                _ => throw new ArgumentException(nameof(roleName))
            };
        }
    }

    public interface IConfigItem
    {
        bool WrapAllItems { get; }
        ItemWrappingPaper WrappingPaper { get; }
    }
}
