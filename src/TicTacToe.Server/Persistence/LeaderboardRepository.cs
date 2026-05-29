using Microsoft.EntityFrameworkCore;
using TicTacToe.Shared.Protocol.Dtos;

namespace TicTacToe.Server.Persistence;

/// <summary>Репозиторий таблицы рекордов.</summary>
public sealed class LeaderboardRepository
{
    private readonly LeaderboardDbContext _db;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LeaderboardRepository(LeaderboardDbContext db)
    {
        _db = db;
    }

    public Task IncrementWinAsync(string nickname, CancellationToken ct = default) =>
        IncrementAsync(nickname, s => s.Wins++, ct);

    public Task IncrementLossAsync(string nickname, CancellationToken ct = default) =>
        IncrementAsync(nickname, s => s.Losses++, ct);

    public Task IncrementDrawAsync(string nickname, CancellationToken ct = default) =>
        IncrementAsync(nickname, s => s.Draws++, ct);

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int limit, CancellationToken ct = default)
    {
        var rows = await _db.Scores
            .OrderByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ThenByDescending(s => s.Draws)
            .Take(limit)
            .ToListAsync(ct).ConfigureAwait(false);

        return rows
            .Select(s => new LeaderboardEntry(s.Nickname, s.Wins, s.Losses, s.Draws))
            .ToList();
    }

    private async Task IncrementAsync(string nickname, Action<Score> mutate, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            throw new ArgumentException("Никнейм не может быть пустым.", nameof(nickname));

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var score = await _db.Scores.FirstOrDefaultAsync(s => s.Nickname == nickname, ct)
                .ConfigureAwait(false);

            if (score is null)
            {
                score = new Score { Nickname = nickname };
                _db.Scores.Add(score);
            }

            mutate(score);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
