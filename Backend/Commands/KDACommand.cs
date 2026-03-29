using Backend.RiotAPI;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Commands
{
    public class KDAModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRiotApi _riotApi;
        private readonly IServiceScopeFactory _scopeFactory;

        public KDAModule(IRiotApi riotApi, IServiceScopeFactory scopeFactory)
        {
            _riotApi = riotApi;
            _scopeFactory = scopeFactory;
        }

        [SlashCommand("kda", "Average KDA of player (Default Region is NA)")]
        public async Task KDA(
            [Summary(description: "IGN and tagline in Faker#NA1 format. Leave blank to use your linked account."), Autocomplete(typeof(PlayerAutocompleteHandler))] string? player = null,
            [Summary(description: "Queue type: normal, solo (ranked solo/duo), or flex (ranked flex). Defaults to normal.")] string queueType = "normal")
        {
            await DeferAsync();

            var (ign, tagline, resolvedPuuid, error) = await PlayerInput.ResolveAsync(player, Context, _scopeFactory, _riotApi);
            if (error != null)
            {
                await FollowupAsync(error);
                return;
            }

            if (!PlayerInput.TryParseQueueType(queueType, out string? queueError))
            {
                await FollowupAsync(queueError!);
                return;
            }

            string puuid = resolvedPuuid ?? await _riotApi.GetRiotPUUID(ign!, tagline!);
            string iconUrl = await _riotApi.GetProfileIconUrlCached(puuid);

            List<string> matchIds = queueType.ToLower() switch
            {
                "solo" => await _riotApi.GetMatchIdsByQueue(puuid, 420),
                "flex" => await _riotApi.GetMatchIdsByQueue(puuid, 440),
                _      => await _riotApi.GetMatchIds(puuid, "normal")
            };

            string avgKDA = await _riotApi.GetAvgKDAFromMatches(matchIds, puuid);

            string queueLabel = queueType.ToLower() switch
            {
                "solo" => "Ranked Solo/Duo",
                "flex" => "Ranked Flex",
                _      => "Normal"
            };

            var embed = new EmbedBuilder()
                .WithTitle($"{ign}#{tagline}")
                .WithThumbnailUrl(iconUrl)
                .AddField("Queue", queueLabel, inline: true)
                .AddField("Matches Analysed", matchIds.Count, inline: true)
                .AddField("Average KDA", avgKDA, inline: true)
                .WithColor(Color.Blue)
                .Build();

            await FollowupAsync(embed: embed);
        }
    }
}
