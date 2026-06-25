using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.AccessControl;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.AccessControl;

public class VisitServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly VisitService _svc;

    public VisitServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new VisitService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── RecordEntry ────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordEntry_CreatesVisitRecord()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        Assert.True(result.IsSuccess);
        var visit = result.Value!;
        Assert.Equal(club.Id, visit.ClubId);
        Assert.Equal(sub.Id, visit.ClientSubscriptionId);
        Assert.Equal(EntryMethod.card, visit.EntryMethod);
        Assert.NotEqual(default, visit.EnteredAt);
        Assert.Null(visit.ExitedAt);
    }

    [Fact]
    public async Task RecordEntry_DecrementsVisitsLeft_WhenLimited()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true, visitsLimit: 10);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.qr);

        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.Equal(9, updated!.VisitsLeft);
    }

    [Fact]
    public async Task RecordEntry_DoesNotDecrementVisits_WhenUnlimited()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true, visitsLimit: null);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.Null(updated!.VisitsLeft);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenSubscriptionNotActive()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.expired,
            StartedAt = DateTime.UtcNow.AddDays(-60),
            ExpiresAt = DateTime.UtcNow.AddDays(-30)
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenExpired()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.active,
            StartedAt = DateTime.UtcNow.AddDays(-60),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        Assert.False(result.IsSuccess);
        Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenNoVisitsLeft()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, visitsLimit: 5);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.active,
            StartedAt = DateTime.UtcNow.AddDays(-5),
            VisitsLeft = 0
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        Assert.False(result.IsSuccess);
        Assert.Contains("no visits", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenClientAlreadyInside()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);
        var result = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);

        Assert.False(result.IsSuccess);
        Assert.Contains("already inside", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenClubNotInSubscription()
    {
        var allowedClub = await Seed.ClubAsync(_db, "Allowed Club");
        var otherClub = await Seed.ClubAsync(_db, "Other Club");
        var client = await Seed.ClientAsync(_db);
        var type = new SubscriptionType
        {
            Name = "Single Club", Price = 100, IsAllClubs = false,
            DurationDays = 30
        };
        type.Clubs.Add(new SubscriptionTypeClub { SubscriptionType = type, Club = allowedClub });
        _db.SubscriptionTypes.Add(type);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.active,
            StartedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(29)
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.RecordEntryAsync(otherClub.Id, sub.Id, EntryMethod.bracelet);

        Assert.False(result.IsSuccess);
        Assert.Contains("not valid for this club", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordEntry_ReturnsFailure_WhenClubNotFound()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.RecordEntryAsync(Guid.NewGuid(), sub.Id, EntryMethod.card);

        Assert.False(result.IsSuccess);
        Assert.Contains("Club not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── RecordExit ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordExit_SetsExitedAt()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var entryResult = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);
        var visitId = entryResult.Value!.Id;

        var result = await _svc.RecordExitAsync(visitId);

        Assert.True(result.IsSuccess);
        var updated = await _db.Visits.FindAsync(visitId);
        Assert.NotNull(updated!.ExitedAt);
    }

    [Fact]
    public async Task RecordExit_ReturnsFailure_WhenAlreadyExited()
    {
        var club = await Seed.ClubAsync(_db);
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var entryResult = await _svc.RecordEntryAsync(club.Id, sub.Id, EntryMethod.card);
        var visitId = entryResult.Value!.Id;
        await _svc.RecordExitAsync(visitId);

        var result = await _svc.RecordExitAsync(visitId);

        Assert.False(result.IsSuccess);
        Assert.Contains("already recorded", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordExit_ReturnsFailure_WhenVisitNotFound()
    {
        var result = await _svc.RecordExitAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetVisits_FiltersByClub()
    {
        var club1 = await Seed.ClubAsync(_db, "Club 1");
        var club2 = await Seed.ClubAsync(_db, "Club 2");
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        await _svc.RecordEntryAsync(club1.Id, sub.Id, EntryMethod.card);
        // exit so client can enter again
        var firstVisit = await _db.Visits.FirstAsync(v => v.ClubId == club1.Id);
        await _svc.RecordExitAsync(firstVisit.Id);
        await _svc.RecordEntryAsync(club2.Id, sub.Id, EntryMethod.qr);

        var result = await _svc.GetVisitsAsync(club1.Id, null, null);

        Assert.Single(result);
        Assert.All(result, v => Assert.Equal(club1.Id, v.ClubId));
    }
}
