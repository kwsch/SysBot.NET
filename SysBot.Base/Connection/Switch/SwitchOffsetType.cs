using System;
using System.Collections.Generic;

namespace SysBot.Base;

/// <summary>
/// Different offset types that can be pointed to for read/write requests.
/// </summary>
public enum SwitchOffsetType
{
    /// <summary>
    /// Heap base offset
    /// </summary>
    Heap,

    /// <summary>
    /// Main NSO base offset
    /// </summary>
    Main,

    /// <summary>
    /// Raw offset (arbitrary)
    /// </summary>
    Absolute,
}

public interface ICommandBuilder
{
    SwitchOffsetType Type { get; }

    byte[] Peek(ulong offset, int length, bool crlf = true);
    byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true);
    byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true);
}

public static class SwitchOffsetTypeUtil
{
    public static readonly HeapCommand Heap = new();
    public static readonly MainCommand Main = new();
    public static readonly AbsoluteCommand Absolute = new();
}

/// <summary>
/// Heap base offset
/// </summary>
public sealed class HeapCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Heap;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.Peek((uint)offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.Poke((uint)offset, data, crlf);
}

/// <summary>
/// Main NSO base offset
/// </summary>
public sealed class MainCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Main;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.PeekMain(offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekMainMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.PokeMain(offset, data, crlf);
}

/// <summary>
/// Raw offset (arbitrary)
/// </summary>
public sealed class AbsoluteCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Absolute;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.PeekAbsolute(offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekAbsoluteMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.PokeAbsolute(offset, data, crlf);
}
