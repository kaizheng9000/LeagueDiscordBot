using Backend.JSONResponseTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend.RiotAPI
{
    internal class RiotApi
    {
        private readonly HttpClient _httpClient = new();

        public RiotApi(string apiKey, string riotApiHeaderName)
        {
            _httpClient.DefaultRequestHeaders.Add(riotApiHeaderName, apiKey);
        }

        public async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            var response = await _httpClient.GetAsync(
                $"{RiotApiEndpoints.AccountByRiotId}{Uri.EscapeDataString(ign)}/{Uri.EscapeDataString(tagline)}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return (string?)JObject.Parse(responseString)["puuid"]
                ?? throw new InvalidOperationException($"No PUUID found for {ign}#{tagline}.");
        }

        public async Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid)
        {
            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.SummonerByPuuid}{puuid}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<RiotAccountDetails>(responseString)
                ?? throw new InvalidOperationException($"Failed to deserialize account details for PUUID {puuid}.");
        }

        public async Task<List<string>> GetMatchIds(string puuid, string queueType)
        {
            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.MatchIds}{puuid}/ids?type={queueType}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<string>>(responseString)
                ?? throw new InvalidOperationException($"Failed to deserialize match IDs for PUUID {puuid}.");
        }

        private async Task<JObject> GetMatchDetails(string matchId)
        {
            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.MatchDetails}{matchId}");
            response.EnsureSuccessStatusCode();

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<string> GetAvgKDAFromMatches(List<string> matchIds, string puuid)
        {
            if (matchIds.Count == 0)
                return "0.00";

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
