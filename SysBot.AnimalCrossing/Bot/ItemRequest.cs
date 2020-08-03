using System.Collections.Generic;

namespace SysBot.AnimalCrossing
{
    public sealed class ItemRequest
    {
        public readonly string User;
        public readonly IReadOnlyCollection<byte[]> Items;

        public ItemRequest(string user, IReadOnlyCollection<byte[]> items)
        {
            User = user;
            Items = items;
        }
    }
}
