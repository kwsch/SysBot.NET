using System.Collections.Generic;
using YouTube.Base;

namespace SysBot.Pokemon.YouTube
{
    class Scopes
    {
        public static List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.ManageAccount,
            OAuthClientScopeEnum.ManageData,
        };
    }
}
