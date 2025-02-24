using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Commands;

namespace Backend.CommandHandlers
{
    internal class HandleSlashCommands
    {
        public static async Task Handler(SocketSlashCommand command)
        {
            switch(command.Data.Name)
            {
                case "kda":
                    await KDACommand.HandleKDACommand(command);
                    break;
                case "facts":
                    await FactsCommand.HandleFactsCommand(command);
                    break;
            }
            
        }
    }
}
