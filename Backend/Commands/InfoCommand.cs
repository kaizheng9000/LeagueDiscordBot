using Backend.RiotAPI;
using Discord;
using Discord.Interactions;

namespace Backend.Commands
{
    public class InfoModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRiotApi _riotApi;

        public InfoModule(IRiotApi riotApi)
        {
            _riotApi = riotApi;
        }

        [SlashCommand("info", "General account info for a player")]
        public async Task Info(
            [Summary(description: "IGN and tagline in Faker#NA1 format."), Autocomplete(typeof(PlayerAutocompleteHandler))] string player)
        {
            await DeferAsync();

            if (!PlayerInput.TryParse(player, out string ign, out string tagline, out string? playerError))
            {
                await FollowupAsync(playerError);
                return;
            }

            string puuid = await _riotApi.GetRiotPUUID(ign, tagline);
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
