using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Subscriptions;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Subscriptions;

public class SubscriptionTypeServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly SubscriptionTypeService _svc;

    public SubscriptionTypeServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new SubscriptionTypeService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeleted()
    {
        await Seed.SubscriptionTypeAsync(_db);
        _db.SubscriptionTypes.Add(new SubscriptionType
        {
            Name = "Deleted", Price = 100, IsAllClubs = true, DeletedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetAllAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_AllClubs_CreatesWithoutClubLinks()
    {
        var result = await _svc.CreateAsync("All-Clubs Pass", 30, null, 5000, isAllClubs: true, null);

        Assert.True(result.IsSuccess);
        var st = result.Value!;
        Assert.True(st.IsAllClubs);
        Assert.Empty(st.Clubs);
    }

    [Fact]
    public async Task CreateAsync_SpecificClubs_LinksToClubs()
    {
        var club1 = await Seed.ClubAsync(_db, "Club 1");
        var club2 = await Seed.ClubAsync(_db, "Club 2");

        var result = await _svc.CreateAsync(
            "Club Pass", 30, null, 2500,
            isAllClubs: false, [club1.Id, club2.Id]);

        Assert.True(result.IsSuccess);
        var links = await _db.SubscriptionTypeClubs
            .Where(x => x.SubscriptionTypeId == result.Value!.Id)
            .ToListAsync();
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenClubNotFound()
    {
        var result = await _svc.CreateAsync(
            "Club Pass", 30, null, 2500,
            isAllClubs: false, [Guid.NewGuid()]);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WithVisitsLimit_SetsLimit()
    {
        var result = await _svc.CreateAsync("10 Visits", null, 10, 1500, true, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.VisitsLimit);
        Assert.Null(result.Value.DurationDays);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        var club = await Seed.ClubAsync(_db);
        var st = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);

        var result = await _svc.UpdateAsync(
            st.Id, "Updated Name", 60, 20, 9999, isAllClubs: false, [club.Id]);

        Assert.True(result.IsSuccess);
        var updated = await _db.SubscriptionTypes.FindAsync(st.Id);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(60, updated.DurationDays);
        Assert.False(updated.IsAllClubs);
    }

    [Fact]
    public async Task UpdateAsync_ClearsOldClubLinks()
    {
        var club1 = await Seed.ClubAsync(_db, "Club 1");
        var club2 = await Seed.ClubAsync(_db, "Club 2");
        var st = await Seed.SubscriptionTypeAsync(_db, isAllClubs: false);
        _db.SubscriptionTypeClubs.Add(new SubscriptionTypeClub { SubscriptionTypeId = st.Id, ClubId = club1.Id });
        await _db.SaveChangesAsync();

        var result = await _svc.UpdateAsync(
            st.Id, st.Name, st.DurationDays, st.VisitsLimit, st.Price,
            isAllClubs: false, [club2.Id]);

        Assert.True(result.IsSuccess);
        var links = await _db.SubscriptionTypeClubs
            .Where(x => x.SubscriptionTypeId == st.Id).ToListAsync();
        Assert.Single(links);
        Assert.Equal(club2.Id, links[0].ClubId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.UpdateAsync(Guid.NewGuid(), "X", null, null, 0, true, null);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_SetsSoftDeleteTimestamp()
    {
        var st = await Seed.SubscriptionTypeAsync(_db);

        var result = await _svc.DeleteAsync(st.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.SubscriptionTypes.IgnoreQueryFilters().FirstAsync(x => x.Id == st.Id);
        Assert.NotNull(raw.DeletedAt);
    }
}
