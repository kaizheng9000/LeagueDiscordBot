using Discord;
using Discord.Net;
using Newtonsoft.Json;
using Discord.WebSocket;

namespace LeagueDiscordBot.CommandBuilders
{
    /// <summary>
    /// Helper class for creating a slash command
    /// </summary>
    internal class BuildSlashCommand
    {  
        /// <summary>
        /// Creates a global slash command for the bot
        /// </summary>
        /// <param name="client"> The client to create the command for </param>
        /// <param name="name"> The name of the command to be created </param>
        /// <param name="description"> A description of the command </param>
        /// <returns> Completes the task or outputs an exception on the console </returns>
        public static async Task CreateGlobalSlashCommand(DiscordSocketClient client, string name, string description = "No Description")
        {
            var globalCommand = new SlashCommandBuilder();
            globalCommand.WithName(name);
            globalCommand.WithDescription(description);
            globalCommand.AddOption("ign", ApplicationCommandOptionType.String, "The IGN to search up.", isRequired: true);
            globalCommand.AddOption("tagline", ApplicationCommandOptionType.String, "The Tag Line of the associated IGN (Exclude the hashtag) (EX: NA1).", isRequired: true);
            globalCommand.AddOption("queuetype", ApplicationCommandOptionType.String, "The queue type to look in, Default is normal rift. Input \"ranked\" if you want to check only ranked.", isRequired: false);

            try
            {
                // With global commands we don't need the guild.
                await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }
    }
}
