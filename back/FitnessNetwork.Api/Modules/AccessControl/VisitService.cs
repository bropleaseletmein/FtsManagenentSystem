using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.AccessControl;

public class VisitService(AppDbContext db)
{
    public async Task<List<Visit>> GetVisitsByClientAsync(Guid clientId) =>
        await db.Visits
            .Include(v => v.Club)
            .Where(v => v.ClientSubscription.ClientId == clientId)
            .OrderByDescending(v => v.EnteredAt)
            .ToListAsync();

    public async Task<List<Visit>> GetVisitsAsync(Guid? clubId, DateTime? from, DateTime? to) =>
        await db.Visits
            .Include(v => v.Club)
            .Include(v => v.ClientSubscription).ThenInclude(cs => cs.Client)
            .Where(v =>
                (!clubId.HasValue || v.ClubId == clubId) &&
                (!from.HasValue || v.EnteredAt >= from) &&
                (!to.HasValue || v.EnteredAt <= to))
            .OrderByDescending(v => v.EnteredAt)
            .ToListAsync();

    public async Task<Result<Visit>> RecordEntryAsync(
        Guid clubId, Guid clientSubscriptionId, EntryMethod entryMethod)
    {
        var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId);
        if (!clubExists) return Result<Visit>.Fail("Club not found.");

        var sub = await db.ClientSubscriptions
            .Include(cs => cs.SubscriptionType).ThenInclude(st => st.Clubs)
            .FirstOrDefaultAsync(cs => cs.Id == clientSubscriptionId);

        if (sub is null) return Result<Visit>.Fail("Subscription not found.");
        if (sub.Status != SubscriptionStatus.active)
            return Result<Visit>.Fail("Subscription is not active.");
        if (sub.ExpiresAt.HasValue && sub.ExpiresAt < DateTime.UtcNow)
            return Result<Visit>.Fail("Subscription has expired.");
        if (sub.VisitsLeft.HasValue && sub.VisitsLeft <= 0)
            return Result<Visit>.Fail("No visits remaining.");

        if (!sub.SubscriptionType.IsAllClubs)
        {
            var allowedClubs = sub.SubscriptionType.Clubs.Select(c => c.ClubId);
            if (!allowedClubs.Contains(clubId))
                return Result<Visit>.Fail("This subscription is not valid for this club.");
        }

        var alreadyInside = await db.Visits.AnyAsync(
            v => v.ClientSubscriptionId == clientSubscriptionId && v.ExitedAt == null);
        if (alreadyInside) return Result<Visit>.Fail("Client is already inside.");

        if (sub.VisitsLeft.HasValue)
            sub.VisitsLeft--;

        var visit = new Visit
        {
            ClubId = clubId,
            ClientSubscriptionId = clientSubscriptionId,
            EntryMethod = entryMethod,
            EnteredAt = DateTime.UtcNow
        };

        db.Visits.Add(visit);
        await db.SaveChangesAsync();
        return Result<Visit>.Ok(visit);
    }

    public async Task<Result<Visit>> RecordExitAsync(Guid visitId)
    {
        var visit = await db.Visits.FindAsync(visitId);
        if (visit is null) return Result<Visit>.Fail("Visit not found.");
        if (visit.ExitedAt.HasValue) return Result<Visit>.Fail("Exit already recorded.");

        visit.ExitedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result<Visit>.Ok(visit);
    }
}
