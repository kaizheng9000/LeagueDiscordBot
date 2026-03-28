using Backend.JSONResponseTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.RiotAPI
{
    internal class RiotApi
    {
        private readonly string _apiKey;
        private readonly string _riotApiHeaderName;

        private static HttpClient sharedClient = new();

        public RiotApi(string apiKey, string riotApiHeaderName)
        {
            _apiKey = apiKey;
            _riotApiHeaderName = riotApiHeaderName;
        }

        public async Task<string> GetRiotPUUID(string ign, string tagline)
        {
            string url = $"{RiotApiEndpoints.AccountByRiotId}{ign}/{tagline}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(_riotApiHeaderName, _apiKey);
            
            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            
            // TODO: I'll handle errors later
            string puuid = (string)JObject.Parse(responseString)["puuid"];
    
            return puuid;
        }

        public async Task<RiotAccountDetails> GetAccountDetailsByPUUID(string puuid)
        {
            string url = $"{RiotApiEndpoints.SummonerByPuuid}{puuid}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(_riotApiHeaderName, _apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // TODO: Handle Errors
            RiotAccountDetails accountDetails = JsonConvert.DeserializeObject<RiotAccountDetails>(responseString);

            return accountDetails;
        }

        public async Task<List<string>> GetMatchIds(string puuid, string queueType)
        {
            string url = $"{RiotApiEndpoints.MatchIds}{puuid}/ids?type={queueType}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(_riotApiHeaderName, _apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            List<string> matchIds = JsonConvert.DeserializeObject<List<string>>(responseString);

            return matchIds;
        }

        private async Task<JObject> GetMatchDetails(string matchId)
        {
            string url = $"{RiotApiEndpoints.MatchDetails}{matchId}";

            HttpRequestMessage request = new();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            request.Headers.Add(_riotApiHeaderName, _apiKey);

            HttpResponseMessage response = await sharedClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseString);

            return jsonResponse;
        }

        public async Task<string> GetAvgKDAFromMatches(List<string> matchIds, string puuid)
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
