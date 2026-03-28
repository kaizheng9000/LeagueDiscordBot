using Discord.Interactions;
using Backend.RiotAPI;
using Backend.JSONResponseTypes;

namespace Backend.Commands
{
    internal class KDAModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IRiotApi _riotApi;

        public KDAModule(IRiotApi riotApi)
        {
            _riotApi = riotApi;
        }

        [SlashCommand("kda", "Average KDA of player (Default Region is NA)")]
        public async Task KDA(
            [Summary(description: "The IGN to search up.")] string ign,
            [Summary(description: "The Tag Line of the associated IGN (Exclude the hashtag) (EX: NA1).")] string tagline,
            [Summary(description: "The queue type to look in. Default is normal rift. Input \"ranked\" for ranked only.")] string queueType = "normal")
        {
            await DeferAsync();

            string puuid = await _riotApi.GetRiotPUUID(ign, tagline);
            RiotAccountDetails account = await _riotApi.GetAccountDetailsByPUUID(puuid);
            List<string> matchIds = await _riotApi.GetMatchIds(puuid, queueType);
            string avgKDA = await _riotApi.GetAvgKDAFromMatches(matchIds, puuid);

            await FollowupAsync($"IGN: {ign} #{tagline} \nKDA: {avgKDA}");
        }
    }
}
