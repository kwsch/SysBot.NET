using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class LedyDistributor<T> where T : PKM, new()
    {
        public readonly Dictionary<string, LedyRequest<T>> UserRequests = new();
        public readonly Dictionary<string, LedyRequest<T>> Distribution;
        public readonly PokemonPool<T> Pool;

        private readonly List<LedyUser> Previous = new();

        public LedyDistributor(PokemonPool<T> pool)
        {
            Pool = pool;
            Distribution = Pool.Files;
        }

        private const Species NoMatchSpecies = Species.None;

        public LedyResponse<T>? GetLedyTrade(T pk, ulong partnerId, Species speciesMatch = NoMatchSpecies)
        {
            if (speciesMatch != NoMatchSpecies && pk.Species != (int)speciesMatch)
                return null;

            var response = GetLedyResponse(pk);
            if (response is null)
                return null;

            if (response.Type != LedyResponseType.MatchRequest)
                return response;

            var previous = Previous.Find(z => z.Recipient == partnerId);
            if (previous is null)
            {
                AddRecipient(partnerId, response);
                return response;
            }

            if (!previous.CanReceive(response))
                return new LedyResponse<T>(response.Receive, LedyResponseType.AbuseDetected);

            UpdateRecipient(previous, response);
            return response;
        }

        private void UpdateRecipient(LedyUser previous, LedyResponse<T> response)
        {
            previous.Requests.Add(response);
        }

        private void AddRecipient(ulong partnerId, LedyResponse<T> response)
        {
            Previous.Add(new LedyUser(partnerId, response));
        }

        private LedyResponse<T>? GetLedyResponse(T pk)
        {
            // All the files should be loaded in as lowercase, regular-width text with no white spaces.
            var nick = StringsUtil.Sanitize(pk.Nickname);
            if (UserRequests.TryGetValue(nick, out var match))
                return new LedyResponse<T>(match.RequestInfo, LedyResponseType.MatchRequest);
            if (Distribution.TryGetValue(nick, out match))
                return new LedyResponse<T>(match.RequestInfo, LedyResponseType.MatchPool);

            return null;
        }

        private sealed class LedyUser
        {
            public readonly ulong Recipient;
            public readonly List<LedyResponse<T>> Requests = new(1);

            public LedyUser(ulong recipient, LedyResponse<T> first)
            {
                Recipient = recipient;
                Requests.Add(first);
            }

            public bool CanReceive(LedyResponse<T> response)
            {
                var poke = response.Receive;
                var prev = Requests.Find(z => ReferenceEquals(z.Receive, poke));
                if (prev is null)
                    return true;

                // Disallow receiving duplicate legends (prevents people farming the bot)
                if (SpeciesCategory.IsLegendary(poke.Species))
                    return false;
                if (SpeciesCategory.IsMythical(poke.Species))
                    return false;
                if (SpeciesCategory.IsSubLegendary(poke.Species))
                    return false;

                return true;
            }
        }
    }
}