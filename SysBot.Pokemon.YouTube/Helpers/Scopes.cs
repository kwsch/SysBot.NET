﻿using System.Collections.Generic;
using YouTube.Base;

namespace SysBot.Pokemon.YouTube
{
    class Scopes
    {
        public readonly static IReadOnlyList<OAuthClientScopeEnum> scopes = new[]
        {
            OAuthClientScopeEnum.ManageAccount,
            OAuthClientScopeEnum.ManageData,
        };
    }
}
