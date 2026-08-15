using System;

namespace SysBot.Pokemon.Discord;

// ReSharper disable once NotAccessedPositionalProperty.Global
public abstract record ChannelAction<T1, T2>(ulong ChannelId, Action<T1, T2> Messager, string ChannelName);
