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
        private readonly HttpClient _plainHttpClient;
        private readonly ILogger<RiotApi> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<(string Ign, string Tagline), string> _puuidCache = new();
        private Dictionary<int, string>? _championIdMap;

        public RiotApi(IHttpClientFactory httpClientFactory, ILogger<RiotApi> logger, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RiotApi");
            _plainHttpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            var key = (ign, tagline);

            if (_puuidCache.TryGetValue(key, out var cachedPuuid))
            {
                _logger.LogDebug("Memory cache hit for {IGN}#{Tagline}", ign, tagline);
                return cachedPuuid;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var cached = await db.Summoners.FirstOrDefaultAsync(s => s.Ign == ign && s.Tagline == tagline);
            if (cached != null)
            {
                _logger.LogDebug("SQLite cache hit for {IGN}#{Tagline}", ign, tagline);
                _puuidCache[key] = cached.Puuid;
                return cached.Puuid;
            }

            _logger.LogDebug("Fetching PUUID for {IGN}#{Tagline}", ign, tagline);

            var response = await _httpClient.GetAsync(
                $"{RiotApiEndpoints.AccountByRiotId}{Uri.EscapeDataString(ign)}/{Uri.EscapeDataString(tagline)}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _puuidCache.Remove(key);
                if (cached != null)
                {
                    db.Summoners.Remove(cached);
                    await db.SaveChangesAsync();
                    _logger.LogInformation("Removed stale cache entry for {IGN}#{Tagline}", ign, tagline);
                }
                throw new InvalidOperationException($"Player {ign}#{tagline} not found.");
            }

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            string puuid = (string?)json["puuid"]
                ?? throw new InvalidOperationException($"No PUUID found for {ign}#{tagline}.");

            db.Summoners.Add(new Summoner { Ign = ign, Tagline = tagline, Puuid = puuid });
            await db.SaveChangesAsync();
            _puuidCache[key] = puuid;

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
            if (cached != null && cached.SummonerId != account.Id)
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

        public async Task<string> GetRank(string puuid)
        {
            _logger.LogDebug("Fetching rank for PUUID {PUUID}", puuid);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.LeagueEntriesByPuuid}{puuid}");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"LeagueEntries returned {(int)response.StatusCode} for puuid={puuid}");

            var entries = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())
                ?? throw new InvalidOperationException($"Failed to deserialize league entries for PUUID {puuid}.");

            static string FormatEntry(JToken? entry, string label)
            {
                if (entry == null) return $"{label}: Unranked";
                string tier = (string?)entry["tier"] ?? "Unranked";
                string rank = (string?)entry["rank"] ?? "";
                int lp = (int)(entry["leaguePoints"] ?? 0);
                int wins = (int)(entry["wins"] ?? 0);
                int losses = (int)(entry["losses"] ?? 0);
                int total = wins + losses;
                string winRate = total > 0 ? $"{(int)Math.Round(wins * 100.0 / total)}%" : "0%";
                return $"{label}: {tier} {rank} — {lp} LP — {wins}W {losses}L ({winRate})";
            }

            var solo = entries.FirstOrDefault(e => (string?)e["queueType"] == "RANKED_SOLO_5x5");
            var flex = entries.FirstOrDefault(e => (string?)e["queueType"] == "RANKED_FLEX_SR");

            return $"{FormatEntry(solo, "Solo/Duo")}\n{FormatEntry(flex, "Flex")}";
        }

        public async Task<string> GetTopChampion(string puuid)
        {
            _logger.LogDebug("Fetching top champion for PUUID {PUUID}", puuid);

            var response = await _httpClient.GetAsync($"{RiotApiEndpoints.TopChampionMastery}{puuid}/top?count=1");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"TopChampionMastery returned {(int)response.StatusCode} for puuid={puuid}");

            var masteries = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())
                ?? throw new InvalidOperationException($"Failed to deserialize champion mastery for PUUID {puuid}.");

            if (!masteries.Any())
                return "None";

            int championId = (int)(masteries[0]["championId"] ?? 0);
            return await GetChampionNameById(championId);
        }

        private async Task<string> GetChampionNameById(int championId)
        {
            if (_championIdMap == null)
            {
                var versionsResponse = await _plainHttpClient.GetAsync(RiotApiEndpoints.DDragonVersions);
                versionsResponse.EnsureSuccessStatusCode();
                var versions = JsonConvert.DeserializeObject<JArray>(await versionsResponse.Content.ReadAsStringAsync());
                string latestVersion = (string?)versions?[0] ?? throw new InvalidOperationException("Failed to fetch Data Dragon versions.");

                var championsResponse = await _plainHttpClient.GetStringAsync(string.Format(RiotApiEndpoints.DDragonChampions, latestVersion));
                var championsJson = JObject.Parse(championsResponse)["data"] as JObject
                    ?? throw new InvalidOperationException("Failed to parse champion data.");

                _championIdMap = championsJson.Properties()
                    .ToDictionary(
                        p => (int)p.Value["key"]!,
                        p => p.Value["name"]!.ToString()
                    );
            }

            return _championIdMap.TryGetValue(championId, out var name) ? name : championId.ToString();
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
