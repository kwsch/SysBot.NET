using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon
{
    public class RemoteControlAccess
    {
        public ulong ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public override string ToString() => $"{Name} = {ID}";
    }

    public class RemoteControlAccessList : List<RemoteControlAccess>
    {
        public bool AllowIfEmpty { get; set; } = true;

        public bool Contains(ulong id) => this.Any(z => z.ID == id);
        public bool Contains(string name) => this.Any(z => z.Name == name);

        public void AddIfNew(IEnumerable<RemoteControlAccess> list)
        {
            foreach (var item in list)
            {
                if (!Contains(item.ID))
                    Add(item);
            }
        }

        public override string ToString() => $"{Count} entries specified.";
    }
}
