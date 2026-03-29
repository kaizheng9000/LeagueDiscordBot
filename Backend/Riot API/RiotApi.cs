using Backend.JSONResponseTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend.RiotAPI
{
    internal class RiotApi : IRiotApi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RiotApi> _logger;

        public RiotApi(IHttpClientFactory httpClientFactory, ILogger<RiotApi> logger)
        {
            _httpClient = httpClientFactory.CreateClient("RiotApi");
            _logger = logger;
        }

        public async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            _logger.LogDebug("Fetching PUUID for {IGN}#{Tagline}", ign, tagline);

            var response = await _httpClient.GetAsync(
                $"{RiotApiEndpoints.AccountByRiotId}{Uri.EscapeDataString(ign)}/{Uri.EscapeDataString(tagline)}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return (string?)JObject.Parse(responseString)["puuid"]
                ?? throw new InvalidOperationException($"No PUUID found for {ign}#{tagline}.");
        }

        public async Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid)
        {
            _logger.LogDebug("Fetching account details for PUUID {PUUID}", puuid);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.SummonerByPuuid}{puuid}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<RiotAccountDetails>(responseString)
                ?? throw new InvalidOperationException($"Failed to deserialize account details for PUUID {puuid}.");
        }

        public async Task<List<string>> GetMatchIds(string puuid, string queueType)
        {
            _logger.LogDebug("Fetching {QueueType} match IDs for PUUID {PUUID}", queueType, puuid);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.MatchIds}{puuid}/ids?type={queueType}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<string>>(responseString)
                ?? throw new InvalidOperationException($"Failed to deserialize match IDs for PUUID {puuid}.");
        }

        private async Task<JObject> GetMatchDetails(string matchId)
        {
            _logger.LogDebug("Fetching match details for {MatchId}", matchId);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.MatchDetails}{matchId}");
            response.EnsureSuccessStatusCode();

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<string> GetAvgKDAFromMatches(List<string> matchIds, string puuid)
        {
            if (matchIds.Count == 0)
                return "0.00";

            _logger.LogInformation("Calculating average KDA across {Count} matches for PUUID {PUUID}", matchIds.Count, puuid);

            var matchDetails = await Task.WhenAll(matchIds.Select(GetMatchDetails));

            float kda = matchDetails.Sum(match =>
            {
                var participants = (JArray)(match.SelectToken("info.participants")
                    ?? throw new InvalidOperationException("Match data missing 'info.participants'."));
                return (float)(participants
                    .First(player => (string?)player["puuid"] == puuid)
                    .SelectToken("challenges.kda")
                    ?? throw new InvalidOperationException("Match data missing 'challenges.kda'."));
            });

            kda /= matchIds.Count;

            return kda.ToString("0.00");
        }
    }
}
