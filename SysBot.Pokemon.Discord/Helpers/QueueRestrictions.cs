using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord;

public static class QueueRestrictions
{
    private const uint MaxTradeCode = 9999_9999;
    private static DiscordManager Manager => SysCordSettings.Manager;

    /// <param name="context">The interaction context.</param>
    extension(IInteractionContext context)
    {
        /// <summary>
        /// Checks if the user provided trade code is valid or empty. If invalid, responds to the interaction with an error message to the user.
        /// </summary>
        /// <param name="code">The trade code to check.</param>
        /// <returns>True if the trade code is valid or empty, false otherwise.</returns>
        public async Task<bool> IsTradeCodeValidOrEmpty(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return true; // can be null or empty, which means random code will be generated

            // Check if it is within the valid range for trade codes (0-99999999)
            if (uint.TryParse(code, out var parsed) && parsed <= MaxTradeCode)
                return true;

            return await context.ReplyBadCodeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Checks if the user provided trade code is valid or empty. If invalid, responds to the interaction with an error message to the user.
        /// </summary>
        /// <param name="code">The trade code to check.</param>
        /// <returns>True if the trade code is valid or empty, false otherwise.</returns>
        public async Task<bool> IsTradeCodeValidOrEmpty(int? code)
        {
            if (code is null)
                return true; // can be null or empty, which means random code will be generated

            // Check if it is within the valid range for trade codes (0-99999999)
            if ((uint)code.Value <= MaxTradeCode)
                return true;

            return await context.ReplyBadCodeAsync().ConfigureAwait(false);
        }

        private async Task<bool> ReplyBadCodeAsync()
        {
            await context.Interaction.RespondAsync($"The trade code must be between 0 and {MaxTradeCode}.", ephemeral: true).ConfigureAwait(false);
            return false;
        }

        public RequestSignificance GetSignificance() => context.User.GetSignificance();
    }

    extension(IUser user)
    {
        /// <summary>
        /// Gets the significance of the user based on their ID and roles.
        /// </summary>
        public RequestSignificance GetSignificance()
        {
            // Check user ID.
            var userId = user.Id;
            if (Manager.IsTeamOrOwner(userId))
                return RequestSignificance.Owner;

            // Don't check Team membership for special favor.

            if (Manager.CanUseSudo(userId))
                return RequestSignificance.Favored;

            // Check roles, might be a special role granted.
            // Stringy names are for user convenience; must trust externally managed guilds the bot is added to (else we should use role IDs).
            return user is SocketGuildUser g
                ? Manager.GetSignificance(g.Roles.Select(z => z.Name))
                : RequestSignificance.None;
        }
    }
}
