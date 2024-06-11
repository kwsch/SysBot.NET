using System;

namespace SysBot.Base;

/// <summary>
/// Bare minimum methods required to interact with a <see cref="IConsoleConnection"/> in a synchronous manner.
/// </summary>
public interface IConsoleConnectionSync : IConsoleConnection
{
    byte[] ReadBytes(uint offset, int length);

    int Send(byte[] buffer);

    void WriteBytes(ReadOnlySpan<byte> data, uint offset);
}
