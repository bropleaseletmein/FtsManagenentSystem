using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Staff;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Staff;

public class StaffServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly StaffService _svc;

    public StaffServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new StaffService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveStaff()
    {
        var club = await Seed.ClubAsync(_db);
        await Seed.StaffMemberAsync(_db, club);
        var deleted = new Api.Data.Entities.Staff
        {
            Club = club, FirstName = "Del", LastName = "User",
            Email = "del@test.com", DeletedAt = DateTime.UtcNow
        };
        _db.Staff.Add(deleted);
        await _db.SaveChangesAsync();

        var result = await _svc.GetAllAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesStaffWithRoles()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.CreateAsync(
            club.Id, "Ivan", "Ivanov", "ivan@test.com",
            [StaffRoleType.trainer], password: null);

        Assert.True(result.IsSuccess);
        var staff = result.Value!;
        Assert.NotEqual(Guid.Empty, staff.Id);
        Assert.Equal("Ivan", staff.FirstName);
        Assert.Single(staff.Roles);
        Assert.Equal(StaffRoleType.trainer, staff.Roles.First().Role);
    }

    [Fact]
    public async Task CreateAsync_CreatesCredentials_WhenPasswordProvided()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.CreateAsync(
            club.Id, "Ivan", "Ivanov", "ivan@test.com",
            [StaffRoleType.admin], password: "Secret123!");

        Assert.True(result.IsSuccess);
        var creds = await _db.StaffCredentials.FirstOrDefaultAsync(c => c.StaffId == result.Value!.Id);
        Assert.NotNull(creds);
        Assert.Equal("ivan@test.com", creds.Email);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenClubNotFound()
    {
        var result = await _svc.CreateAsync(
            Guid.NewGuid(), "Ivan", "Ivanov", "ivan@test.com",
            [StaffRoleType.trainer], password: null);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_SupportsDualRole()
    {
        var club = await Seed.ClubAsync(_db);

        var result = await _svc.CreateAsync(
            club.Id, "Oleg", "Admin", "oleg@test.com",
            [StaffRoleType.admin, StaffRoleType.trainer], password: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Roles.Count);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club);

        var result = await _svc.UpdateAsync(staff.Id, "Petr", "Petrov", "p.petrov@test.com");

        Assert.True(result.IsSuccess);
        var updated = await _db.Staff.FindAsync(staff.Id);
        Assert.Equal("Petr", updated!.FirstName);
        Assert.Equal("p.petrov@test.com", updated.Email);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.UpdateAsync(Guid.NewGuid(), "X", "X", "x@test.com");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_SetsSoftDeleteTimestamp()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club);

        var result = await _svc.DeleteAsync(staff.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.Staff.IgnoreQueryFilters().FirstAsync(s => s.Id == staff.Id);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task AddRoleAsync_AddsNewRole()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club, StaffRoleType.trainer);

        var result = await _svc.AddRoleAsync(staff.Id, StaffRoleType.admin);

        Assert.True(result.IsSuccess);
        var roles = await _db.StaffRoles.Where(r => r.StaffId == staff.Id).ToListAsync();
        Assert.Equal(2, roles.Count);
    }

    [Fact]
    public async Task AddRoleAsync_ReturnsFailure_WhenRoleAlreadyAssigned()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club, StaffRoleType.trainer);

        var result = await _svc.AddRoleAsync(staff.Id, StaffRoleType.trainer);

        Assert.False(result.IsSuccess);
        Assert.Contains("already", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveRoleAsync_RemovesRole()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club, StaffRoleType.trainer);

        var result = await _svc.RemoveRoleAsync(staff.Id, StaffRoleType.trainer);

        Assert.True(result.IsSuccess);
        var roles = await _db.StaffRoles.Where(r => r.StaffId == staff.Id).ToListAsync();
        Assert.Empty(roles);
    }

    [Fact]
    public async Task RemoveRoleAsync_ReturnsFailure_WhenRoleNotFound()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club, StaffRoleType.trainer);

        var result = await _svc.RemoveRoleAsync(staff.Id, StaffRoleType.admin);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SetCredentialsAsync_CreatesCredentials_WhenNoneExist()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club);

        var result = await _svc.SetCredentialsAsync(staff.Id, "new@test.com", "Pass123!");

        Assert.True(result.IsSuccess);
        var creds = await _db.StaffCredentials.FirstOrDefaultAsync(c => c.StaffId == staff.Id);
        Assert.NotNull(creds);
        Assert.Equal("new@test.com", creds!.Email);
    }

    [Fact]
    public async Task SetCredentialsAsync_UpdatesExistingCredentials()
    {
        var club = await Seed.ClubAsync(_db);
        var staff = await Seed.StaffMemberAsync(_db, club);
        _db.StaffCredentials.Add(new StaffCredentials
        {
            Staff = staff,
            Email = "old@test.com",
            PasswordHash = "oldhash",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _svc.SetCredentialsAsync(staff.Id, "updated@test.com", "NewPass!");

        Assert.True(result.IsSuccess);
        var creds = await _db.StaffCredentials.FirstAsync(c => c.StaffId == staff.Id);
        Assert.Equal("updated@test.com", creds.Email);
        Assert.NotEqual("oldhash", creds.PasswordHash);
    }
}
