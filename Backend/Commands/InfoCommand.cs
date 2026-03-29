using Backend.RiotAPI;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Commands
{
    public class InfoModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRiotApi _riotApi;
        private readonly IServiceScopeFactory _scopeFactory;

        public InfoModule(IRiotApi riotApi, IServiceScopeFactory scopeFactory)
        {
            _riotApi = riotApi;
            _scopeFactory = scopeFactory;
        }

        [SlashCommand("info", "General account info for a player")]
        public async Task Info(
            [Summary(description: "IGN and tagline in Faker#NA1 format. Leave blank to use your linked account."), Autocomplete(typeof(PlayerAutocompleteHandler))] string? player = null)
        {
            await DeferAsync();

            var (ign, tagline, resolvedPuuid, error) = await PlayerInput.ResolveAsync(player, Context, _scopeFactory, _riotApi);
            if (error != null)
            {
                await FollowupAsync(error);
                return;
            }

            string puuid = resolvedPuuid ?? await _riotApi.GetRiotPUUID(ign!, tagline!);
            var account = await _riotApi.GetAccountDetailsByPUUID(puuid);
            string rank = await _riotApi.GetRank(puuid);
            var topChampions = await _riotApi.GetTopChampions(puuid);
            string iconUrl = await _riotApi.GetProfileIconUrl(account.ProfileIconId);

            var embed = new EmbedBuilder()
                .WithTitle($"{ign}#{tagline}")
                .WithThumbnailUrl(iconUrl)
                .AddField("Level", account.SummonerLevel, inline: true)
                .AddField("Most Played", string.Join(", ", topChampions), inline: true)
                .AddField("Rank", rank, inline: false)
                .WithColor(Color.Gold)
                .Build();

            await FollowupAsync(embed: embed);
        }
    }
}
