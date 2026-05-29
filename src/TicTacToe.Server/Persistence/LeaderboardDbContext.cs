using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Server.Persistence;

public sealed class LeaderboardDbContext : DbContext
{
    public DbSet<Score> Scores => Set<Score>();

    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Score>(b =>
        {
            b.HasIndex(x => x.Nickname).IsUnique();
        });
    }
}
