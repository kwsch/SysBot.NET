using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class RemoteControlAccess
    {
        public ulong ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public override string ToString() => $"{Name} = {ID}";
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class RemoteControlAccessList
    {
        public List<RemoteControlAccess> List { get; set; } = new();
        public bool AllowIfEmpty { get; set; } = true;

        public bool Contains(ulong id) => List.Any(z => z.ID == id);
        public bool Contains(string name) => List.Any(z => z.Name == name);
        public int RemoveAll(Predicate<RemoteControlAccess> item) => List.RemoveAll(item);
        public void Clear() => List.Clear();

        public void AddIfNew(IEnumerable<RemoteControlAccess> list)
        {
            foreach (var item in list)
            {
                if (!Contains(item.ID))
                    List.Add(item);
            }
        }

        public IEnumerator<RemoteControlAccess> GetEnumerator() => List.GetEnumerator();

        public override string ToString()
        {
            return List.Count == 0
                ? (AllowIfEmpty ? "Anyone allowed" : "None allowed (none specified).")
                : $"{List.Count} entries specified.";
        }
    }
}
