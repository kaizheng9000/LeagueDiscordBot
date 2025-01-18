using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueDiscordBot.JSONResponseTypes
{
    public class RiotAccountDetails
    {
        string Id { get; set; } // This is called the encrypted summoner id on riots docs
        string AccountId { get; set; }
        string PuuId { get; set; }
        string ProfileIconId { get; set; }
        string RevisionDate { get; set; }
        string SummonerLevel { get; set; }

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
