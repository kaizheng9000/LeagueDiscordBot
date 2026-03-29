namespace Backend.Database
{
    public class Summoner
    {
        public string Ign { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Puuid { get; set; } = string.Empty;
        public string SummonerId { get; set; } = string.Empty;
        public int ProfileIconId { get; set; }
    }
}
