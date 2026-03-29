using Microsoft.EntityFrameworkCore;

namespace Backend.Database
{
    public class BotDbContext : DbContext
    {
        public DbSet<Summoner> Summoners { get; set; }
        public DbSet<LinkedAccount> LinkedAccounts { get; set; }

        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Summoner>()
                .HasKey(s => new { s.Ign, s.Tagline });

            modelBuilder.Entity<LinkedAccount>()
                .HasKey(l => l.DiscordUserId);
        }
    }
}
