using Backend.JSONResponseTypes;
using Backend.RiotAPI;
using Discord.Interactions;

namespace Backend.Commands
{
    public class KDAModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRiotApi _riotApi;

        public KDAModule(IRiotApi riotApi)
        {
            _riotApi = riotApi;
        }

        [SlashCommand("kda", "Average KDA of player (Default Region is NA)")]
        public async Task KDA(
            [Summary(description: "IGN and tagline in Faker#NA1 format."), Autocomplete(typeof(PlayerAutocompleteHandler))] string player,
            [Summary(description: "The queue type to look in. Default is normal rift. Input \"ranked\" for ranked only.")] string queueType = "normal")
        {
            await DeferAsync();

            if (!PlayerInput.TryParse(player, out string ign, out string tagline))
            {
                await FollowupAsync("Invalid format. Please use `IGN#Tagline` (e.g. `Faker#NA1`).");
                return;
            }

            string puuid = await _riotApi.GetRiotPUUID(ign, tagline);
            RiotAccountDetails account = await _riotApi.GetAccountDetailsByPUUID(puuid);
            List<string> matchIds = await _riotApi.GetMatchIds(puuid, queueType);
            string avgKDA = await _riotApi.GetAvgKDAFromMatches(matchIds, puuid);

            await FollowupAsync($"IGN: {ign} #{tagline} \nKDA: {avgKDA}");
        }
    }

}
