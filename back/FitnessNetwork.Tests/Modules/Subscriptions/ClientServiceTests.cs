using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Subscriptions;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Subscriptions;

public class ClientServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly ClientService _svc;

    public ClientServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new ClientService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveClients()
    {
        await Seed.ClientAsync(_db);
        _db.Clients.Add(new Client
        {
            FirstName = "Del", LastName = "User",
            Email = "del@test.com", DeletedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetAllAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesClient()
    {
        var result = await _svc.CreateAsync(
            "Anna", "Ivanova", "anna@test.com", "+79001234567",
            new DateOnly(1995, 5, 15), password: null);

        Assert.True(result.IsSuccess);
        var c = result.Value!;
        Assert.NotEqual(Guid.Empty, c.Id);
        Assert.Equal("Anna", c.FirstName);
        Assert.Equal("anna@test.com", c.Email);
    }

    [Fact]
    public async Task CreateAsync_CreatesCredentials_WhenEmailAndPasswordProvided()
    {
        var result = await _svc.CreateAsync(
            "Anna", "Ivanova", "anna@test.com", null, null, "Secret123!");

        Assert.True(result.IsSuccess);
        var creds = await _db.ClientCredentials
            .FirstOrDefaultAsync(c => c.ClientId == result.Value!.Id);
        Assert.NotNull(creds);
    }

    [Fact]
    public async Task CreateAsync_DoesNotCreateCredentials_WhenPasswordMissing()
    {
        var result = await _svc.CreateAsync("Bob", "Smith", "bob@test.com", null, null, password: null);

        Assert.True(result.IsSuccess);
        var creds = await _db.ClientCredentials
            .FirstOrDefaultAsync(c => c.ClientId == result.Value!.Id);
        Assert.Null(creds);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var client = await Seed.ClientAsync(_db);

        var result = await _svc.UpdateAsync(
            client.Id, "Новое", "Имя", "new@test.com", "+79999999999",
            new DateOnly(2000, 1, 1));

        Assert.True(result.IsSuccess);
        var updated = await _db.Clients.FindAsync(client.Id);
        Assert.Equal("Новое", updated!.FirstName);
        Assert.Equal("new@test.com", updated.Email);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.UpdateAsync(Guid.NewGuid(), "X", "X", null, null, null);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_SetsSoftDeleteTimestamp()
    {
        var client = await Seed.ClientAsync(_db);

        var result = await _svc.DeleteAsync(client.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.Clients.IgnoreQueryFilters().FirstAsync(c => c.Id == client.Id);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task DeletedClient_NotReturnedInGetAll()
    {
        var client = await Seed.ClientAsync(_db);
        await _svc.DeleteAsync(client.Id);

        Assert.Empty(await _svc.GetAllAsync());
    }

    [Fact]
    public async Task SetCredentialsAsync_CreatesCredentials()
    {
        var client = await Seed.ClientAsync(_db);

        var result = await _svc.SetCredentialsAsync(client.Id, "cred@test.com", "Pass123!");

        Assert.True(result.IsSuccess);
        var creds = await _db.ClientCredentials.FirstAsync(c => c.ClientId == client.Id);
        Assert.Equal("cred@test.com", creds.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify("Pass123!", creds.PasswordHash));
    }

    [Fact]
    public async Task SetCredentialsAsync_UpdatesExistingCredentials()
    {
        var client = await Seed.ClientAsync(_db);
        _db.ClientCredentials.Add(new ClientCredentials
        {
            Client = client,
            Email = "old@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass", 4),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _svc.SetCredentialsAsync(client.Id, "new@test.com", "NewPass!");

        Assert.True(result.IsSuccess);
        var creds = await _db.ClientCredentials.FirstAsync(c => c.ClientId == client.Id);
        Assert.Equal("new@test.com", creds.Email);
    }

    [Fact]
    public async Task SetCredentialsAsync_ReturnsFailure_WhenClientNotFound()
    {
        var result = await _svc.SetCredentialsAsync(Guid.NewGuid(), "x@test.com", "Pass!");
        Assert.False(result.IsSuccess);
    }
}
