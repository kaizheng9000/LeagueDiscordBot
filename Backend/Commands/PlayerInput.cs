using Backend.Database;
using Backend.RiotAPI;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Commands
{
    internal static class PlayerInput
    {
        internal static async Task<(string? ign, string? tagline, string? puuid, string? error)> ResolveAsync(
            string? player, IInteractionContext context, IServiceScopeFactory scopeFactory, IRiotApi riotApi)
        {
            if (string.IsNullOrWhiteSpace(player))
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                var linked = await db.LinkedAccounts.FirstOrDefaultAsync(
                    l => l.DiscordUserId == context.User.Id.ToString());

                if (linked == null)
                    return (null, null, null, "You don't have a linked account. Use `/link` to set one, or provide a player name.");

                var (currentIgn, currentTagline) = await riotApi.GetCurrentName(linked.Puuid);
                if (currentIgn != linked.Ign || currentTagline != linked.Tagline)
                {
                    linked.Ign = currentIgn;
                    linked.Tagline = currentTagline;
                    await db.SaveChangesAsync();
                }

                return (currentIgn, currentTagline, linked.Puuid, null);
            }

            if (!TryParse(player, out string ign, out string tagline, out string? error))
                return (null, null, null, error);

            return (ign, tagline, null, null);
        }
        private static readonly string[] ValidQueueTypes = ["normal", "ranked"];

        internal static bool TryParse(string player, out string ign, out string tagline, out string? error)
        {
            var parts = player.Split('#', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                ign = string.Empty;
                tagline = string.Empty;
                error = "Invalid format. Please use `IGN#Tagline` (e.g. `Faker#NA1`).";
                return false;
            }

            ign = parts[0].Trim();
            tagline = parts[1].Trim();

            if (ign.Length < 3 || ign.Length > 16)
            {
                error = $"`{ign}` is not a valid IGN. Must be between 3 and 16 characters.";
                return false;
            }

            if (tagline.Length < 2 || tagline.Length > 5)
            {
                error = $"`{tagline}` is not a valid tagline. Must be between 2 and 5 characters.";
                return false;
            }

            error = null;
            return true;
        }

        internal static bool TryParseQueueType(string queueType, out string? error)
        {
            if (!ValidQueueTypes.Contains(queueType.ToLower()))
            {
                error = $"Invalid queue type `{queueType}`. Valid options are: `normal`, `ranked`.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
