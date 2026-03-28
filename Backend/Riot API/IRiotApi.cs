using Backend.JSONResponseTypes;

namespace Backend.RiotAPI
{
    internal interface IRiotApi
    {
        Task<string> GetRiotPUUID(string ign, string tagline);
        Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid);
        Task<List<string>> GetMatchIds(string puuid, string queueType);
        Task<string> GetAvgKDAFromMatches(List<string> matchIds, string puuid);
    }
}
