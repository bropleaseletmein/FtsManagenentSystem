using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Subscriptions;

public class SubscriptionService(AppDbContext db)
{
    public async Task<List<ClientSubscription>> GetByClientAsync(Guid clientId) =>
        await db.ClientSubscriptions
            .Include(cs => cs.SubscriptionType)
            .Where(cs => cs.ClientId == clientId)
            .OrderByDescending(cs => cs.StartedAt)
            .ToListAsync();

    public async Task<ClientSubscription?> GetByIdAsync(Guid id) =>
        await db.ClientSubscriptions
            .Include(cs => cs.SubscriptionType)
            .Include(cs => cs.Client)
            .Include(cs => cs.Freezes)
            .FirstOrDefaultAsync(cs => cs.Id == id);

    public async Task<Result<ClientSubscription>> SellAsync(Guid clientId, Guid subscriptionTypeId)
    {
        var clientExists = await db.Clients.AnyAsync(c => c.Id == clientId);
        if (!clientExists) return Result<ClientSubscription>.Fail("Client not found.");

        var st = await db.SubscriptionTypes.FindAsync(subscriptionTypeId);
        if (st is null) return Result<ClientSubscription>.Fail("Subscription type not found.");

        var now = DateTime.UtcNow;
        var cs = new ClientSubscription
        {
            ClientId = clientId,
            SubscriptionTypeId = subscriptionTypeId,
            Status = SubscriptionStatus.active,
            StartedAt = now,
            ExpiresAt = st.DurationDays.HasValue ? now.AddDays(st.DurationDays.Value) : null,
            VisitsLeft = st.VisitsLimit
        };

        db.ClientSubscriptions.Add(cs);
        await db.SaveChangesAsync();
        return Result<ClientSubscription>.Ok(cs);
    }

    public async Task<Result> ChangeStatusAsync(Guid id, SubscriptionStatus newStatus)
    {
        var cs = await db.ClientSubscriptions.FindAsync(id);
        if (cs is null) return Result.Fail("Subscription not found.");

        cs.Status = newStatus;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<SubscriptionFreeze>> FreezeAsync(Guid subscriptionId, DateOnly startedAt)
    {
        var cs = await db.ClientSubscriptions.FindAsync(subscriptionId);
        if (cs is null) return Result<SubscriptionFreeze>.Fail("Subscription not found.");
        if (cs.Status != SubscriptionStatus.active)
            return Result<SubscriptionFreeze>.Fail("Only active subscriptions can be frozen.");

        cs.Status = SubscriptionStatus.frozen;

        var freeze = new SubscriptionFreeze
        {
            ClientSubscriptionId = subscriptionId,
            StartedAt = startedAt
        };

        db.SubscriptionFreezes.Add(freeze);
        await db.SaveChangesAsync();
        return Result<SubscriptionFreeze>.Ok(freeze);
    }

    public async Task<Result> UnfreezeAsync(Guid subscriptionId)
    {
        var cs = await db.ClientSubscriptions
            .Include(x => x.Freezes)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId);

        if (cs is null) return Result.Fail("Subscription not found.");
        if (cs.Status != SubscriptionStatus.frozen)
            return Result.Fail("Subscription is not frozen.");

        var activeFreeze = cs.Freezes.FirstOrDefault(f => f.EndedAt is null);
        if (activeFreeze is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var days = today.DayNumber - activeFreeze.StartedAt.DayNumber;
            activeFreeze.EndedAt = today;
            activeFreeze.DaysFrozen = days;

            if (cs.ExpiresAt.HasValue)
                cs.ExpiresAt = cs.ExpiresAt.Value.AddDays(days);
        }

        cs.Status = SubscriptionStatus.active;
        await db.SaveChangesAsync();
        return Result.Ok();
    }
}
