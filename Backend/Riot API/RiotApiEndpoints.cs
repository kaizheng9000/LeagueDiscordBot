namespace Backend.RiotAPI
{
    internal static class RiotApiEndpoints
    {
        public const string AccountByRiotId = "https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/"; // Would need to make the region dynamic, can use active shard ep for the region maybe
        public const string SummonerByPuuid = "https://na1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/";
        public const string MatchIds = "https://americas.api.riotgames.com/lol/match/v5/matches/by-puuid/"; // Append "/ids" after the puuid to get matches
        public const string MatchDetails = "https://americas.api.riotgames.com/lol/match/v5/matches/";
    }
}
