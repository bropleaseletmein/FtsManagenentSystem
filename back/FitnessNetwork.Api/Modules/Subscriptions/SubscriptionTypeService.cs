using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Subscriptions;

public class SubscriptionTypeService(AppDbContext db)
{
    public async Task<List<SubscriptionType>> GetAllAsync() =>
        await db.SubscriptionTypes.Include(st => st.Clubs).OrderBy(st => st.Name).ToListAsync();

    public async Task<SubscriptionType?> GetByIdAsync(Guid id) =>
        await db.SubscriptionTypes.Include(st => st.Clubs).FirstOrDefaultAsync(st => st.Id == id);

    public async Task<Result<SubscriptionType>> CreateAsync(
        string name, int? durationDays, int? visitsLimit, decimal price,
        bool isAllClubs, IEnumerable<Guid>? clubIds)
    {
        var st = new SubscriptionType
        {
            Name = name,
            DurationDays = durationDays,
            VisitsLimit = visitsLimit,
            Price = price,
            IsAllClubs = isAllClubs
        };

        if (!isAllClubs && clubIds is not null)
        {
            foreach (var clubId in clubIds.Distinct())
            {
                var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId);
                if (!clubExists) return Result<SubscriptionType>.Fail($"Club {clubId} not found.");
                st.Clubs.Add(new SubscriptionTypeClub { SubscriptionType = st, ClubId = clubId });
            }
        }

        db.SubscriptionTypes.Add(st);
        await db.SaveChangesAsync();
        return Result<SubscriptionType>.Ok(st);
    }

    public async Task<Result> UpdateAsync(
        Guid id, string name, int? durationDays, int? visitsLimit, decimal price,
        bool isAllClubs, IEnumerable<Guid>? clubIds)
    {
        var st = await db.SubscriptionTypes.Include(x => x.Clubs).FirstOrDefaultAsync(x => x.Id == id);
        if (st is null) return Result.Fail("Subscription type not found.");

        st.Name = name;
        st.DurationDays = durationDays;
        st.VisitsLimit = visitsLimit;
        st.Price = price;
        st.IsAllClubs = isAllClubs;

        db.SubscriptionTypeClubs.RemoveRange(st.Clubs);
        st.Clubs.Clear();

        if (!isAllClubs && clubIds is not null)
        {
            foreach (var clubId in clubIds.Distinct())
                st.Clubs.Add(new SubscriptionTypeClub { SubscriptionTypeId = id, ClubId = clubId });
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var st = await db.SubscriptionTypes.FindAsync(id);
        if (st is null) return Result.Fail("Subscription type not found.");

        st.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }
}
