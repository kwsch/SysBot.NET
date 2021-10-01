using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class TrackedUserLog
    {
        private const int Capacity = 1000;
        private readonly List<TrackedUser> Users = new(Capacity);
        private readonly object _sync = new();
        private int ReplaceIndex;

        public TrackedUser? TryRegister(ulong networkID, string name, ulong remoteID)
        {
            if (remoteID == 0)
                return null;

            lock (_sync)
                return InsertReplace(networkID, name, remoteID);
        }

        public TrackedUser? TryRegister(ulong networkID, string name)
        {
            lock (_sync)
                return InsertReplace(networkID, name);
        }

        private TrackedUser? InsertReplace(ulong networkID, string name)
        {
            var index = Users.FindIndex(z => z.ID == networkID);
            if (index < 0)
            {
                Insert(networkID, name, 0);
                return null;
            }

            var match = Users[index];
            Users[index] = new TrackedUser(networkID, name, 0);
            return match;
        }

        private TrackedUser? InsertReplace(ulong networkID, string name, ulong remoteID)
        {
            var index = Users.FindIndex(z => z.ID == networkID);
            if (index < 0)
            {
                Insert(networkID, name, remoteID);
                return null;
            }

            var match = Users[index];
            if (match.RemoteID != remoteID) // different user triggered this
            {
                Users[index] = new TrackedUser(networkID, name, remoteID);
                return match;
            }

            return null;
        }

        private void Insert(ulong id, string name, ulong remoteID)
        {
            var user = new TrackedUser(id, name, remoteID);
            if (Users.Count != Capacity)
            {
                Users.Add(user);
                return;
            }

            Users[ReplaceIndex] = user;
            ReplaceIndex = (ReplaceIndex + 1) % Capacity;
        }

        public TrackedUser? TryGetPrevious(ulong trainerNid)
        {
            lock (_sync)
                return Users.Find(z => z.ID == trainerNid);
        }
    }

    public sealed record TrackedUser
    {
        public readonly string Name;
        public readonly ulong RemoteID;
        public readonly ulong ID;
        public readonly DateTime Time;

        public TrackedUser(ulong id, string name, ulong remoteID)
        {
            ID = id;
            Name = name;
            RemoteID = remoteID;
            Time = DateTime.Now;
        }
    }
}
