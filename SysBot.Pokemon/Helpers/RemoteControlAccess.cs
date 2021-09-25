using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class RemoteControlAccessList : IEnumerable<RemoteControlAccess>
    {
        public bool AllowIfEmpty { get; set; } = true;
        public List<RemoteControlAccess> List { get; set; } = new();

        public bool Contains(ulong id) => List.Any(z => z.ID == id);
        public bool Contains(string name) => List.Any(z => z.Name == name);
        public int RemoveAll(Predicate<RemoteControlAccess> item) => List.RemoveAll(item);
        public void Clear() => List.Clear();
        public int Count => List.Count;

        public void AddIfNew(IEnumerable<RemoteControlAccess> list)
        {
            foreach (var item in list)
            {
                if (!Contains(item.ID))
                    List.Add(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<RemoteControlAccess> GetEnumerator() => List.GetEnumerator();

        public override string ToString()
        {
            return Count == 0
                ? (AllowIfEmpty ? "Anyone allowed" : "None allowed (none specified).")
                : $"{List.Count} entries specified.";
        }
    }
}
