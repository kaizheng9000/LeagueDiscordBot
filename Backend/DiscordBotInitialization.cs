using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Backend.RiotAPI;

namespace Backend
{
    public class DiscordBotInitialization
    {
        public static async Task Main()
        {
            await Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("config.json", false, true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<InteractionService>();
                    services.AddHttpClient("RiotApi", (sp, client) =>
                    {
                        var config = sp.GetRequiredService<IConfiguration>();
                        client.DefaultRequestHeaders.Add(
                            config["RiotAPIHeaderName"] ?? throw new InvalidOperationException("RiotAPIHeaderName is missing from config.json"),
                            config["RiotAPIToken"] ?? throw new InvalidOperationException("RiotAPIToken is missing from config.json")
                        );
                    });
                    services.AddSingleton<IRiotApi, RiotApi>();
                    services.AddHostedService<BotService>();
                })
                .Build()
                .RunAsync();
        }
    }
}
