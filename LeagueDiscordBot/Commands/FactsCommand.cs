using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueDiscordBot.Commands
{
    internal class FactsCommand
    {
        public static async Task HandleFactsCommand(SocketSlashCommand command)
        {
            await command.RespondAsync($"Facts Command Handler, {command.Data.Options.First().Value}");
        }
    }
}
