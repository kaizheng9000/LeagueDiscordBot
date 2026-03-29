using Backend.RiotAPI;
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
                await FollowupAsync(playerError!);
                return;
            }

            string puuid = await _riotApi.GetRiotPUUID(ign, tagline);
            var account = await _riotApi.GetAccountDetailsByPUUID(puuid);
            string rank = await _riotApi.GetRank(puuid);
            string topChampion = await _riotApi.GetTopChampion(puuid);

            await FollowupAsync(
                $"**{ign}#{tagline}**\n" +
                $"Level: {account.SummonerLevel}\n" +
                $"Rank: {rank}\n" +
                $"Most Played: {topChampion}");
        }
    }
}
