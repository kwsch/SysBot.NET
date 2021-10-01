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

        public override string ToString() => $"{Name} = {ID} // {Comment}";
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class RemoteControlAccessList
    {
        /// <summary>
        /// Don't mutate this list; use <see cref="AddIfNew"/> and <see cref="RemoveAll"/>.
        /// This is public for serialization purposes.
        /// </summary>
        public List<RemoteControlAccess> List { get; set; } = new();

        /// <summary>
        /// Allows Bot-Specific role lists to permit anyone to use the bot if there are no roles specified.
        /// </summary>
        /// <remarks>
        /// Also used by Channel White-lists. No channels whitelisted &amp; true => any channel can be used for bot commands.
        /// </remarks>
        public bool AllowIfEmpty { get; set; } = true;

        /// <summary> Checks if any item has the requested <see cref="id"/> property. </summary>
        /// <returns>True if any item matches the <see cref="id"/></returns>
        /// <remarks>Used for unique IDs such as Users or Channels</remarks>
        public bool Contains(ulong id) => List.Any(z => z.ID == id);

        /// <summary> Checks if any item has the requested <see cref="name"/> property. </summary>
        /// <returns>True if any item matches the <see cref="name"/></returns>
        /// <remarks>Used for checking Role Names</remarks>
        public bool Contains(string name) => List.Any(z => z.Name == name);

        public int RemoveAll(Predicate<RemoteControlAccess> item) => List.RemoveAll(item);
        public void Clear() => List.Clear();

        /// <summary>
        /// Adds new items if not already present by <see cref="RemoteControlAccess.ID"/>.
        /// </summary>
        /// <param name="list">List of items to add</param>
        public void AddIfNew(IEnumerable<RemoteControlAccess> list)
        {
            foreach (var item in list)
            {
                if (!Contains(item.ID))
                    List.Add(item);
            }
        }

        /// <summary>
        /// Used with foreach iterating. You shouldn't be calling this manually.
        /// </summary>
        public IEnumerator<RemoteControlAccess> GetEnumerator() => List.GetEnumerator();

        /// <summary>
        /// Used for summarizing this object in a PropertyGrid.
        /// </summary>
        public override string ToString()
        {
            return List.Count == 0
                ? (AllowIfEmpty ? "Anyone allowed" : "None allowed (none specified).")
                : $"{List.Count} entries specified.";
        }

        /// <summary>
        /// Gets an enumerable summary of the items in this object.
        /// </summary>
        public IEnumerable<string> Summarize() => List.Select(z => z.ToString());
    }
}
