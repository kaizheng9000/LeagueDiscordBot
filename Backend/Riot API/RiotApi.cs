using Backend.Database;
using Backend.JSONResponseTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend.RiotAPI
{
    internal class RiotApi : IRiotApi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RiotApi> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RiotApi(IHttpClientFactory httpClientFactory, ILogger<RiotApi> logger, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RiotApi");
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var cached = await db.Summoners.FirstOrDefaultAsync(s => s.Ign == ign && s.Tagline == tagline);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for {IGN}#{Tagline}", ign, tagline);
                return cached.Puuid;
            }

            _logger.LogDebug("Fetching PUUID for {IGN}#{Tagline}", ign, tagline);

            var response = await _httpClient.GetAsync(
                $"{RiotApiEndpoints.AccountByRiotId}{Uri.EscapeDataString(ign)}/{Uri.EscapeDataString(tagline)}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            string puuid = (string?)json["puuid"]
                ?? throw new InvalidOperationException($"No PUUID found for {ign}#{tagline}.");

            db.Summoners.Add(new Summoner { Ign = ign, Tagline = tagline, Puuid = puuid });
            await db.SaveChangesAsync();

            return puuid;
        }

        public async Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid)
        {
            _logger.LogDebug("Fetching account details for PUUID {PUUID}", puuid);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.SummonerByPuuid}{puuid}");
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<RiotAccountDetails>(responseString)
                ?? throw new InvalidOperationException($"Failed to deserialize account details for PUUID {puuid}.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var cached = await db.Summoners.FirstOrDefaultAsync(s => s.Puuid == puuid);
            if (cached != null && (cached.Ign != account.Id || cached.SummonerId != account.Id))
            {
                _logger.LogInformation("Summoner data changed for PUUID {PUUID}, updating cache", puuid);
                cached.SummonerId = account.Id;
                await db.SaveChangesAsync();
            }

            return account;
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

            var matchDetails = new List<JObject>();
            foreach (var matchId in matchIds)
                matchDetails.Add(await GetMatchDetails(matchId));

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
