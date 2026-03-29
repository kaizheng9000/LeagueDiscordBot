using Backend.Database;
using Backend.RiotAPI;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Commands
{
    public class LinkModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRiotApi _riotApi;

        public LinkModule(IServiceScopeFactory scopeFactory, IRiotApi riotApi)
        {
            _scopeFactory = scopeFactory;
            _riotApi = riotApi;
        }

        [SlashCommand("link", "Link your Discord account to your League IGN")]
        public async Task Link(
            [Summary(description: "Your IGN and tagline in Faker#NA1 format.")] string player)
        {
            await DeferAsync(ephemeral: true);

            if (!PlayerInput.TryParse(player, out string ign, out string tagline, out string? error))
            {
                await FollowupAsync(error, ephemeral: true);
                return;
            }

            string puuid = await _riotApi.GetRiotPUUID(ign, tagline);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var userId = Context.User.Id.ToString();
            var existing = await db.LinkedAccounts.FirstOrDefaultAsync(l => l.DiscordUserId == userId);

            if (existing != null)
            {
                existing.Puuid = puuid;
                existing.Ign = ign;
                existing.Tagline = tagline;
            }
            else
            {
                db.LinkedAccounts.Add(new LinkedAccount { DiscordUserId = userId, Puuid = puuid, Ign = ign, Tagline = tagline });
            }

            await db.SaveChangesAsync();
            await FollowupAsync($"Linked to `{ign}#{tagline}`.", ephemeral: true);
        }

        [SlashCommand("unlink", "Unlink your Discord account from your League IGN")]
        public async Task Unlink()
        {
            await DeferAsync(ephemeral: true);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var userId = Context.User.Id.ToString();
            var existing = await db.LinkedAccounts.FirstOrDefaultAsync(l => l.DiscordUserId == userId);

            if (existing == null)
            {
                await FollowupAsync("You don't have a linked account.", ephemeral: true);
                return;
            }

            db.LinkedAccounts.Remove(existing);
            await db.SaveChangesAsync();
            await FollowupAsync("Account unlinked.", ephemeral: true);
        }
    }
}
