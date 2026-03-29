using Backend.Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Backend
{
    internal class BotService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;
        private readonly ILogger<BotService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public BotService(
            DiscordSocketClient client,
            InteractionService interactionService,
            IServiceProvider services,
            IConfiguration config,
            ILogger<BotService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _interactionService = interactionService;
            _services = services;
            _config = config;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }

            _client.Log += LogDiscord;
            _interactionService.Log += LogDiscord;

            _interactionService.InteractionExecuted += async (_, ctx, result) =>
            {
                if (!result.IsSuccess)
                {
                    _logger.LogError("Command failed [{InteractionId}]: {Error}", ctx.Interaction.Id, result.ErrorReason);
                    await ctx.Interaction.FollowupAsync($"Something went wrong: {result.ErrorReason}");
                }
            };

            _client.Ready += async () =>
            {
                await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
                await _interactionService.RegisterCommandsGloballyAsync();
            };

            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            };

            await _client.LoginAsync(TokenType.Bot,
                _config["DiscordBotToken"] ?? throw new InvalidOperationException("DiscordBotToken is missing from config.json"));
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private Task LogDiscord(LogMessage msg)
        {
            var level = msg.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error    => LogLevel.Error,
                LogSeverity.Warning  => LogLevel.Warning,
                LogSeverity.Info     => LogLevel.Information,
                LogSeverity.Verbose  => LogLevel.Trace,
                LogSeverity.Debug    => LogLevel.Debug,
                _                    => LogLevel.Information
            };
            _logger.Log(level, msg.Exception, "{Message}", msg.Message);
            return Task.CompletedTask;
        }
    }
}
