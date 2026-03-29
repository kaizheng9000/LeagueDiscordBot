namespace Backend.RiotAPI
{
    internal static class RiotApiEndpoints
    {
        public const string AccountByRiotId = "https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/"; // Would need to make the region dynamic, can use active shard ep for the region maybe
        public const string AccountByPuuid = "https://americas.api.riotgames.com/riot/account/v1/accounts/by-puuid/";
        public const string SummonerByPuuid = "https://na1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/";
        public const string MatchIds = "https://americas.api.riotgames.com/lol/match/v5/matches/by-puuid/"; // Append "/ids" after the puuid to get matches
        public const string MatchDetails = "https://americas.api.riotgames.com/lol/match/v5/matches/";
        public const string LeagueEntriesByPuuid = "https://na1.api.riotgames.com/lol/league/v4/entries/by-puuid/";
        public const string TopChampionMastery = "https://na1.api.riotgames.com/lol/champion-mastery/v4/champion-masteries/by-puuid/"; // Append "{puuid}/top?count=1"
        public const string DDragonVersions = "https://ddragon.leagueoflegends.com/api/versions.json";
        public const string DDragonChampions = "https://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion.json";
    }
}
