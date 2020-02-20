using System.Collections.Generic;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class LedyDistributor<T> where T : PKM, new()
    {
        public readonly Dictionary<string, LedyRequest<T>> UserRequests = new Dictionary<string, LedyRequest<T>>();
        public readonly Dictionary<string, LedyRequest<T>> Distribution;
        public readonly PokemonPool<T> Pool;

        public LedyDistributor(PokemonPool<T> pool)
        {
            Pool = pool;
            Distribution = Pool.Files;
        }

        private const Species NoMatchSpecies = Species.None;

        public LedyResponse<T> GetLedyTrade(T pk, Species speciesMatch = NoMatchSpecies)
        {
            if (speciesMatch != NoMatchSpecies && pk.Species != (int)speciesMatch)
                return GetRandomResponse();

            var nick = pk.Nickname;
            if (UserRequests.TryGetValue(nick, out var match))
                return new LedyResponse<T>(match.RequestInfo, LedyResponseType.MatchRequest);
            if (Distribution.TryGetValue(nick, out match))
                return new LedyResponse<T>(match.RequestInfo, LedyResponseType.MatchPool);

            return GetRandomResponse();
        }

        private LedyResponse<T> GetRandomResponse()
        {
            var gift = Pool.GetRandomPoke();
            return new LedyResponse<T>(gift, LedyResponseType.Random);
        }
    }
}