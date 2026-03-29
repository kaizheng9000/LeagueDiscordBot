using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.JSONResponseTypes
{
    public class RiotAccountDetails
    {
        public string Id { get; set; } // This is called the encrypted summoner id on riots docs
        public string AccountId { get; set; }
        public string PuuId { get; set; }
        public string ProfileIconId { get; set; }
        public string RevisionDate { get; set; }
        public string SummonerLevel { get; set; }

        public RiotAccountDetails(string id = "", string accountId = "", string puuId = "", string profileIconId = "", string revisionDate = "", string summonerLevel = "")
        {
            Id = id;
            AccountId = accountId;
            PuuId = puuId;
            ProfileIconId = profileIconId;
            RevisionDate = revisionDate;
            SummonerLevel = summonerLevel;
        }
    }
}
