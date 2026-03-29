using Backend.Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
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
        private readonly HttpClient _webhookClient = new();

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
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS LinkedAccounts (
                        DiscordUserId TEXT NOT NULL PRIMARY KEY,
                        Puuid TEXT NOT NULL DEFAULT '',
                        Ign TEXT NOT NULL,
                        Tagline TEXT NOT NULL
                    )", cancellationToken);
            }

            _client.Log += LogDiscord;
            _interactionService.Log += LogDiscord;

            _interactionService.InteractionExecuted += async (_, ctx, result) =>
            {
                if (!result.IsSuccess)
                {
                    _logger.LogError("Command failed [{InteractionId}]: {Error}", ctx.Interaction.Id, result.ErrorReason);
                    await ctx.Interaction.FollowupAsync($"Something went wrong: {result.ErrorReason}");
                    await PostToErrorWebhook(ctx, result);
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

        private static readonly string[] ClapTrapIntros =
        [
            "MINION! Something broke!",
            "Aaaand it's broken.",
            "I did NOT do that.",
            "This is fine. IT'S NOT FINE.",
            "Oh no. Oh no no no.",
            "I blame the minion.",
            "Fuck.",
            "NOT MY FAULT.",
            "I'm... gonna need a minute.",
            "WHAT DID YOU DO?",
            "This is why we can't have nice things.",
        ];

        private async Task PostToErrorWebhook(IInteractionContext ctx, IResult result)
        {
            var webhookUrl = _config["ErrorWebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl)) return;

            var user = ctx.User.Username;
            string command = "unknown";
            string parameters = "";

            if (ctx.Interaction is SocketSlashCommand slash)
            {
                command = $"/{slash.CommandName}";
                var options = slash.Data.Options;
                if (options.Count > 0)
                    parameters = string.Join(", ", options.Select(o => $"{o.Name}={o.Value}"));
            }

            var detail = result is ExecuteResult { Exception: not null } execResult
                ? GetExceptionDetail(execResult.Exception)
                : result.ErrorReason;

            var intro = ClapTrapIntros[Random.Shared.Next(ClapTrapIntros.Length)];
            var paramLine = string.IsNullOrEmpty(parameters) ? "" : $"\nParams: `{parameters}`";
            var message = $"{intro}\n**Command failed**\nUser: `{user}`\nCommand: `{command}`{paramLine}\n```\n{detail}\n```";

            await _webhookClient.PostAsJsonAsync(webhookUrl, new { content = message });
        }

        private static string GetExceptionDetail(Exception ex)
        {
            var root = ex.InnerException ?? ex;
            var method = root.TargetSite;
            var location = method != null
                ? $"{method.DeclaringType?.Name}.{method.Name}"
                : "unknown";
            return $"{root.GetType().Name}: {root.Message}\nat {location}";
        }

        private Task LogDiscord(LogMessage msg)
        {
            var level = msg.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            };
            _logger.Log(level, msg.Exception, "{Message}", msg.Message);
            return Task.CompletedTask;
        }
    }
}
