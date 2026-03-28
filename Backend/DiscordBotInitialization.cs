using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Backend.RiotAPI;

namespace Backend
{
    public class DiscordBotInitialization
    {
        public static async Task Main()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", false, true)
                .Build();

            var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<RiotApi>(_ => new RiotApi(
                    config["RiotAPIToken"] ?? throw new InvalidOperationException("RiotAPIToken is missing from config.json"),
                    config["RiotAPIHeaderName"] ?? throw new InvalidOperationException("RiotAPIHeaderName is missing from config.json")
                ))
                .BuildServiceProvider();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var interactionService = services.GetRequiredService<InteractionService>();

            client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
            interactionService.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };

            client.Ready += async () =>
            {
                await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
                await interactionService.RegisterCommandsGloballyAsync();
            };

            client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(client, interaction);
                await interactionService.ExecuteCommandAsync(ctx, services);
            };

            await client.LoginAsync(TokenType.Bot,
                config["DiscordBotToken"] ?? throw new InvalidOperationException("DiscordBotToken is missing from config.json"));
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
