using Newtonsoft.Json;

namespace Backend.JSONResponseTypes
{
    public class RiotAccountDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty; // Encrypted summoner ID

        [JsonProperty("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonProperty("puuid")]
        public string PuuId { get; set; } = string.Empty;

        [JsonProperty("profileIconId")]
        public int ProfileIconId { get; set; }

        [JsonProperty("revisionDate")]
        public long RevisionDate { get; set; }

        [JsonProperty("summonerLevel")]
        public int SummonerLevel { get; set; }
    }
}
