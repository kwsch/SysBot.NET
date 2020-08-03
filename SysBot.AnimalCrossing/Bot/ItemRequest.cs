using System.Collections.Generic;

namespace SysBot.AnimalCrossing
{
    public sealed class ItemRequest
    {
        public readonly string User;
        public readonly IReadOnlyCollection<Item> Items;

        public ItemRequest(string user, IReadOnlyCollection<Item> items)
        {
            User = user;
            Items = items;
        }
    }
}
