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

            var parts = player.Split('#', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                await FollowupAsync("Invalid format. Please use `IGN#Tagline` (e.g. `Faker#NA1`).");
                return;
            }

            string ign = parts[0].Trim();
            string tagline = parts[1].Trim();

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
