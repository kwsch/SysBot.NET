using System.Collections.Generic;
using YouTube.Base;

namespace SysBot.Pokemon.YouTube;

internal static class Scopes
{
    public static readonly IReadOnlyList<OAuthClientScopeEnum> scopes = new[]
    {
        OAuthClientScopeEnum.ManageAccount,
        OAuthClientScopeEnum.ManageData,
    };
}
