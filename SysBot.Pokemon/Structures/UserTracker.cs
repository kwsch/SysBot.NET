using System;
using System.Collections.Generic;

namespace SysBot.Pokemon;

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

    private TrackedUser? InsertReplace(ulong networkID, string name, ulong remoteID = 0)
    {
        var index = Users.FindIndex(z => z.NetworkID == networkID);
        if (index < 0)
        {
            Insert(networkID, name, remoteID);
            return null;
        }

        var match = Users[index];
        Users[index] = new TrackedUser(networkID, name, remoteID);
        return match;
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

    public void RemoveAllNID(ulong networkID)
    {
        lock (_sync)
            Users.RemoveAll(z => z.NetworkID == networkID);
    }

    public void RemoveAllRemoteID(ulong remoteID)
    {
        lock (_sync)
            Users.RemoveAll(z => z.RemoteID == remoteID);
    }

    public TrackedUser? TryGetPreviousNID(ulong trainerNid)
    {
        lock (_sync)
            return Users.Find(z => z.NetworkID == trainerNid);
    }

    public TrackedUser? TryGetPreviousRemoteID(ulong remoteNid)
    {
        lock (_sync)
            return Users.Find(z => z.RemoteID == remoteNid);
    }

    public IEnumerable<string> Summarize()
    {
        lock (_sync)
            return Users.FindAll(z => z.NetworkID != 0).ConvertAll(z => $"{z.Name}, ID: {z.NetworkID}, Remote ID: {z.RemoteID}");
    }
}

public sealed record TrackedUser
{
    public readonly string Name;
    public readonly ulong RemoteID;
    public readonly ulong NetworkID;
    public readonly DateTime Time;

    public TrackedUser(ulong NetworkID, string name, ulong remoteID)
    {
        this.NetworkID = NetworkID;
        Name = name;
        RemoteID = remoteID;
        Time = DateTime.Now;
    }
}
