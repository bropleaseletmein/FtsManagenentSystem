using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Clubs;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Clubs;

public class ClubServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly ClubService _svc;

    public ClubServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new ClubService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Clubs ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllClubs_ReturnsOnlyNonDeleted()
    {
        await Seed.ClubAsync(_db, "Active Club");
        var deleted = new Club { Name = "Deleted Club", Address = "X", DeletedAt = DateTime.UtcNow };
        _db.Clubs.Add(deleted);
        await _db.SaveChangesAsync();

        var result = await _svc.GetAllClubsAsync();

        Assert.Single(result);
        Assert.Equal("Active Club", result[0].Name);
    }

    [Fact]
    public async Task GetAllClubs_ReturnsEmpty_WhenNone()
    {
        var result = await _svc.GetAllClubsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateClub_CreatesAndReturnsClub()
    {
        var club = await _svc.CreateClubAsync("New Club", "Main St 1", "+7-900-000-0001");

        Assert.NotEqual(Guid.Empty, club.Id);
        Assert.Equal("New Club", club.Name);
        Assert.Equal("Main St 1", club.Address);
        Assert.Equal("+7-900-000-0001", club.Phone);
    }

    [Fact]
    public async Task UpdateClub_UpdatesFields()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.UpdateClubAsync(club.Id, "Updated Name", "New Address", "+7-900-999-9999");

        Assert.True(result.IsSuccess);
        var updated = await _db.Clubs.FindAsync(club.Id);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("New Address", updated.Address);
    }

    [Fact]
    public async Task UpdateClub_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.UpdateClubAsync(Guid.NewGuid(), "X", "X", null);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteClub_SetsSoftDeleteTimestamp()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.DeleteClubAsync(club.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.Clubs.IgnoreQueryFilters().FirstAsync(c => c.Id == club.Id);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task DeleteClub_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.DeleteClubAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeletedClub_NotReturnedInGetAll()
    {
        var club = await Seed.ClubAsync(_db);
        await _svc.DeleteClubAsync(club.Id);

        var result = await _svc.GetAllClubsAsync();

        Assert.Empty(result);
    }

    // ── Halls ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateHall_CreatesInExistingClub()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.CreateHallAsync(club.Id, "Main Hall", 30);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        Assert.Equal("Main Hall", result.Value.Name);
        Assert.Equal(club.Id, result.Value.ClubId);
    }

    [Fact]
    public async Task CreateHall_ReturnsFailure_WhenClubNotFound()
    {
        var result = await _svc.CreateHallAsync(Guid.NewGuid(), "Hall", 20);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetHallsByClub_ReturnsOnlyClubHalls()
    {
        var club1 = await Seed.ClubAsync(_db, "Club 1");
        var club2 = await Seed.ClubAsync(_db, "Club 2");
        await Seed.HallAsync(_db, club1, "Hall A");
        await Seed.HallAsync(_db, club1, "Hall B");
        await Seed.HallAsync(_db, club2, "Hall X");

        var result = await _svc.GetHallsByClubAsync(club1.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, h => Assert.Equal(club1.Id, h.ClubId));
    }

    [Fact]
    public async Task GetHallsByClub_ExcludesSoftDeletedHalls()
    {
        var club = await Seed.ClubAsync(_db);
        await Seed.HallAsync(_db, club, "Active Hall");
        var deleted = new Hall { Club = club, Name = "Deleted Hall", Capacity = 10, DeletedAt = DateTime.UtcNow };
        _db.Halls.Add(deleted);
        await _db.SaveChangesAsync();

        var result = await _svc.GetHallsByClubAsync(club.Id);

        Assert.Single(result);
        Assert.Equal("Active Hall", result[0].Name);
    }

    [Fact]
    public async Task UpdateHall_UpdatesFields()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club, "Old Name", 10);

        var result = await _svc.UpdateHallAsync(hall.Id, "New Name", 50);

        Assert.True(result.IsSuccess);
        var updated = await _db.Halls.FindAsync(hall.Id);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(50, updated.Capacity);
    }

    [Fact]
    public async Task DeleteHall_SetsSoftDeleteTimestamp()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);

        var result = await _svc.DeleteHallAsync(hall.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.Halls.IgnoreQueryFilters().FirstAsync(h => h.Id == hall.Id);
        Assert.NotNull(raw.DeletedAt);
    }
}
