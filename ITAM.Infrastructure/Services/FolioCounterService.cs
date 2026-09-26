using ITAM.Domain.Entities;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Services;

public sealed class FolioCounterService : IFolioCounterService
{
    private readonly ApplicationDbContext _db;

    public FolioCounterService(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefijo de folio requerido.", nameof(prefix));

        var normalized = prefix.Trim().ToUpperInvariant();
        var year = DateTime.UtcNow.Year;
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var counter = await _db.FolioCounters
            .FromSqlInterpolated(
                $@"SELECT * FROM dbo.FolioCounters WITH (UPDLOCK, HOLDLOCK)
                   WHERE Prefix = {normalized} AND [Year] = {year}")
            .FirstOrDefaultAsync(ct);

        if (counter is null)
        {
            counter = new FolioCounter
            {
                Prefix = normalized,
                Year = year,
                LastNumber = 0,
                PadLength = 4,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.FolioCounters.Add(counter);
            await _db.SaveChangesAsync(ct);
        }

        counter.LastNumber += 1;
        counter.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var pad = counter.PadLength <= 0 ? 4 : counter.PadLength;
        return $"{normalized}-{year}-{counter.LastNumber.ToString().PadLeft(pad, '0')}";
    }
}
