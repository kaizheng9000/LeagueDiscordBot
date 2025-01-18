using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueDiscordBot.Riot_API;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using LeagueDiscordBot.JSONResponseTypes;
using Discord.Commands;

namespace LeagueDiscordBot.Commands
{
    internal class KDACommand
    {
        public static async Task HandleKDACommand(SocketSlashCommand command)
        {
            if (command.Data.Options == null)
            {
                await command.RespondAsync("Please provide an ign with the command.");
                return;
            }

            var findIgn = command.Data.Options.FirstOrDefault(param => param.Name == "ign");
            var findTagline = command.Data.Options.FirstOrDefault(param => param.Name == "tagline");
            var findQueueType = command.Data.Options.FirstOrDefault(param => param.Name == "queuetype") == null ? "normal" : "ranked";

            if (findIgn != null && findTagline != null)
            {   
                string ign = findIgn.Value.ToString();
                string tagline = findTagline.Value.ToString();
                string queueType = findQueueType;

                await command.RespondAsync($"Calculating KDA for past 20 {queueType} games...");

                Task<string> getPuuid =  RiotApi.GetRiotPUUID(ign, tagline);
                string puuid = await getPuuid;

                Task<RiotAccountDetails> getAccountDetails = RiotApi.GetAccountDetailsByPUUID(puuid);
                RiotAccountDetails account = await getAccountDetails;

                // Logic for match data here
                Task<List<string>> getMatchIds = RiotApi.GetMatchIds(puuid, queueType);
                List<string> matchIds = await getMatchIds;

                // Need to loop through each match id and gather the avg kda for all 20 games
                Task<string> getAvgKDA = RiotApi.GetAvgKDAFromMatches(matchIds, puuid);
                string avgKDA = await getAvgKDA; 
                
                string response = $"IGN: {ign} #{tagline} \nKDA: {avgKDA}";

                // This should return a KDA text of the past 20 games (that's the idea at least)
                await command.FollowupAsync(response);
            }

            
        }
    }
}
