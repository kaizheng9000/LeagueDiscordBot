using Discord.Interactions;

namespace Backend.Commands
{
    public class FactsModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("facts", "Spits some facts")]
        public async Task Facts()
        {
            await RespondAsync("Facts Command Handler");
        }
    }
}
