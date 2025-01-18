using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using Discord.Interactions;
using Discord.Net;
using Newtonsoft.Json;
using LeagueDiscordBot.CommandHandlers;
using LeagueDiscordBot.CommandBuilders;
using Microsoft.Extensions.Configuration;

namespace LeagueDiscordBot
{

    /// <summary>
    /// Contains the initialization of the bot and command handlers
    /// </summary>
    public class DiscordBotInitialization
    {
        private static DiscordSocketClient Client;
        public static IConfigurationRoot Configs { get; private set; }

        public static async Task Main()
        {
            var build = new ConfigurationBuilder();
            build.SetBasePath(Directory.GetCurrentDirectory());
            build.AddJsonFile("config.json", false, true);
            Configs = build.Build();

            Client = new();
            Client.Log += Log;
            Client.Ready += Client_Ready;
            Client.SlashCommandExecuted += HandleSlashCommands.Handler;

            await Client.LoginAsync(TokenType.Bot, Configs["BotToken"]);
            await Client.StartAsync();

            // Keep client open forever
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            // Can invoke a logging framework here instead of outputting to the console
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static async Task Client_Ready()
        {
           //await BuildSlashCommand.CreateGlobalSlashCommand(Client, "facts", "Spits some facts");
           await BuildSlashCommand.CreateGlobalSlashCommand(Client, "kda", "Average KDA of player (Default Region is NA)");
        }

    }


 




}