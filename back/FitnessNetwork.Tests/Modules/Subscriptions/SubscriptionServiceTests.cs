using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Subscriptions;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Subscriptions;

public class SubscriptionServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly SubscriptionService _svc;

    public SubscriptionServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new SubscriptionService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── SellAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SellAsync_CreatesActiveSubscription()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, durationDays: 30);

        var result = await _svc.SellAsync(client.Id, type.Id);

        Assert.True(result.IsSuccess);
        var cs = result.Value!;
        Assert.Equal(SubscriptionStatus.active, cs.Status);
        Assert.NotNull(cs.StartedAt);
    }

    [Fact]
    public async Task SellAsync_SetsExpiresAt_WhenDurationDaysIsSet()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, durationDays: 30);

        var result = await _svc.SellAsync(client.Id, type.Id);

        Assert.True(result.IsSuccess);
        var cs = result.Value!;
        Assert.NotNull(cs.ExpiresAt);
        Assert.True(cs.ExpiresAt!.Value > DateTime.UtcNow.AddDays(29));
        Assert.True(cs.ExpiresAt!.Value < DateTime.UtcNow.AddDays(31));
    }

    [Fact]
    public async Task SellAsync_DoesNotSetExpiresAt_WhenNoDurationDays()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, durationDays: null);

        var result = await _svc.SellAsync(client.Id, type.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ExpiresAt);
    }

    [Fact]
    public async Task SellAsync_SetsVisitsLeft_WhenVisitsLimitIsSet()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, visitsLimit: 10);

        var result = await _svc.SellAsync(client.Id, type.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.VisitsLeft);
    }

    [Fact]
    public async Task SellAsync_SetsVisitsLeftToNull_WhenUnlimited()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, visitsLimit: null);

        var result = await _svc.SellAsync(client.Id, type.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.VisitsLeft);
    }

    [Fact]
    public async Task SellAsync_ReturnsFailure_WhenClientNotFound()
    {
        var type = await Seed.SubscriptionTypeAsync(_db);

        var result = await _svc.SellAsync(Guid.NewGuid(), type.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("client", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SellAsync_ReturnsFailure_WhenSubscriptionTypeNotFound()
    {
        var client = await Seed.ClientAsync(_db);

        var result = await _svc.SellAsync(client.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("subscription type", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── FreezeAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task FreezeAsync_FreezesActiveSubscription()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _svc.FreezeAsync(sub.Id, startDate);

        Assert.True(result.IsSuccess);
        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.frozen, updated!.Status);
    }

    [Fact]
    public async Task FreezeAsync_CreatesFreezRecord()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _svc.FreezeAsync(sub.Id, startDate);

        Assert.True(result.IsSuccess);
        var freeze = await _db.SubscriptionFreezes
            .FirstOrDefaultAsync(f => f.ClientSubscriptionId == sub.Id);
        Assert.NotNull(freeze);
        Assert.Equal(startDate, freeze!.StartedAt);
        Assert.Null(freeze.EndedAt);
    }

    [Fact]
    public async Task FreezeAsync_ReturnsFailure_WhenNotActive()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.frozen,
            StartedAt = DateTime.UtcNow.AddDays(-5)
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.FreezeAsync(sub.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.IsSuccess);
        Assert.Contains("active", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FreezeAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.FreezeAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.False(result.IsSuccess);
    }

    // ── UnfreezeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UnfreezeAsync_RestoresActiveStatus()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.frozen,
            StartedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20)
        };
        _db.ClientSubscriptions.Add(sub);
        _db.SubscriptionFreezes.Add(new SubscriptionFreeze
        {
            ClientSubscription = sub,
            StartedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
        });
        await _db.SaveChangesAsync();

        var result = await _svc.UnfreezeAsync(sub.Id);

        Assert.True(result.IsSuccess);
        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.active, updated!.Status);
    }

    [Fact]
    public async Task UnfreezeAsync_ExtendsExpiresAt_ByFrozenDays()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var originalExpiry = DateTime.UtcNow.AddDays(20);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.frozen,
            StartedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = originalExpiry
        };
        _db.ClientSubscriptions.Add(sub);
        _db.SubscriptionFreezes.Add(new SubscriptionFreeze
        {
            ClientSubscription = sub,
            StartedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))
        });
        await _db.SaveChangesAsync();

        await _svc.UnfreezeAsync(sub.Id);

        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.True(updated!.ExpiresAt > originalExpiry.AddDays(6));
    }

    [Fact]
    public async Task UnfreezeAsync_SetsEndedAtOnFreeze()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.frozen,
            StartedAt = DateTime.UtcNow.AddDays(-10)
        };
        _db.ClientSubscriptions.Add(sub);
        var freeze = new SubscriptionFreeze
        {
            ClientSubscription = sub,
            StartedAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3))
        };
        _db.SubscriptionFreezes.Add(freeze);
        await _db.SaveChangesAsync();

        await _svc.UnfreezeAsync(sub.Id);

        var updatedFreeze = await _db.SubscriptionFreezes.FindAsync(freeze.Id);
        Assert.NotNull(updatedFreeze!.EndedAt);
        Assert.NotNull(updatedFreeze.DaysFrozen);
        Assert.True(updatedFreeze.DaysFrozen >= 3);
    }

    [Fact]
    public async Task UnfreezeAsync_ReturnsFailure_WhenNotFrozen()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.UnfreezeAsync(sub.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("frozen", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnfreezeAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.UnfreezeAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
    }

    // ── ChangeStatusAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatusAsync_ChangesStatus()
    {
        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.ChangeStatusAsync(sub.Id, SubscriptionStatus.cancelled);

        Assert.True(result.IsSuccess);
        var updated = await _db.ClientSubscriptions.FindAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.cancelled, updated!.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.ChangeStatusAsync(Guid.NewGuid(), SubscriptionStatus.expired);
        Assert.False(result.IsSuccess);
    }
}
