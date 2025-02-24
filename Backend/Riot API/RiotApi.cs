using Discord.Commands;
using Backend.JSONResponseTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Backend;

namespace Backend.RiotAPI
{
    internal class RiotApi
    {
        // Endpoints
        private const string lolAccountEP = "https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/"; // Would need to make the region dynamic, can use active shard ep for the region maybe
        private const string summonerByPuuidEP = "https://na1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/";
        private const string matchIdsEP = "https://americas.api.riotgames.com/lol/match/v5/matches/by-puuid/"; // Append "/ids" after the puuid to get matches
        private const string matchDetailsEP = "https://americas.api.riotgames.com/lol/match/v5/matches/";

        // Grabbing token and header name for riot api
        private static readonly string apiKey = DiscordBotInitialization.Configs["RiotAPIToken"];
        private static readonly string riotApiHeaderName = DiscordBotInitialization.Configs["RiotAPIHeaderName"];

        private static HttpClient sharedClient = new();

        public static async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            string url = $"{lolAccountEP}{ign}/{tagline}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(riotApiHeaderName, apiKey);
            
            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            
            // TODO: I'll handle errors later
            string puuid = (string)JObject.Parse(responseString)["puuid"];
    
            return puuid;
        }

        public static async Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid)
        {
            string url = $"{summonerByPuuidEP}{puuid}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(riotApiHeaderName, apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // TODO: Handle Errors
            RiotAccountDetails accountDetails = JsonConvert.DeserializeObject<RiotAccountDetails>(responseString);

            return accountDetails;
        }

        public static async Task<List<string>> GetMatchIds(string puuid, string queueType)
        {
            string url = $"{matchIdsEP}{puuid}/ids?type={queueType}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(riotApiHeaderName , apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            List<string> matchIds = JsonConvert.DeserializeObject<List<string>>(responseString);

            return matchIds;
        }

        private static async Task<JObject> GetMatchDetails(string matchId)
        {
            string url = $"{matchDetailsEP}{matchId}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(riotApiHeaderName, apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseString);

            return jsonResponse;
        }

        public static async Task<string> GetAvgKDAFromMatches(List<string> matchIds, string puuid)
        {
            float kda = 0;

            await Task.Run(async () =>
            { 
                foreach(string matchId in matchIds) 
                {
                    Task<JObject> getMatchDetails = GetMatchDetails(matchId);
                    JObject matchDetails = await getMatchDetails;
                    // grab kda         
                    var matchInfo = 
                        JArray.Parse(JsonConvert.SerializeObject(matchDetails.SelectToken("info").SelectToken("participants")))
                        .Where(player => (string)player["puuid"] == puuid).ToList();
                         
                    float stat = (float)matchInfo.Select(info => info["challenges"]).Select(challenge => challenge["kda"]).ToList()[0];

                    kda += stat;
                }
            });

            kda /= 20;
            
            return kda.ToString("0.00");
        }

    }
}
