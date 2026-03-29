using Backend.Database;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Commands
{
    public class PlayerAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var input = (autocompleteInteraction.Data.Current.Value as string) ?? string.Empty;

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var lowerInput = input.ToLower();
            var results = await db.Summoners
                .Where(s => (s.Ign + "#" + s.Tagline).ToLower().Contains(lowerInput))
                .Take(25)
                .Select(s => new AutocompleteResult($"{s.Ign}#{s.Tagline}", $"{s.Ign}#{s.Tagline}"))
                .ToListAsync();

            return AutocompletionResult.FromSuccess(results);
        }
    }
}
