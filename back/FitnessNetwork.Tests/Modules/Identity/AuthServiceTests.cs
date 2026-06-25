using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Identity;
using FitnessNetwork.Common.Jwt;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Identity;

public class AuthServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly AuthService _svc;

    // Pre-computed bcrypt hash with work factor 4 for speed
    private const string TestPassword = "TestPass123!";
    private static readonly string TestPasswordHash =
        BCrypt.Net.BCrypt.HashPassword(TestPassword, workFactor: 4);

    public AuthServiceTests()
    {
        _db = DbFactory.Create();

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-that-is-at-least-32-characters-long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresInMinutes = 60
        });
        _svc = new AuthService(_db, new JwtService(jwtSettings));
    }

    public void Dispose() => _db.Dispose();

    // ── Staff Login ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginStaff_ReturnsToken_WhenCredentialsAreValid()
    {
        var (staff, _) = await CreateStaffWithCredentials("admin@test.com", [StaffRoleType.admin]);

        var result = await _svc.LoginStaffAsync("admin@test.com", TestPassword);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
    }

    [Fact]
    public async Task LoginStaff_ReturnsToken_ForTrainer()
    {
        await CreateStaffWithCredentials("trainer@test.com", [StaffRoleType.trainer]);

        var result = await _svc.LoginStaffAsync("trainer@test.com", TestPassword);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginStaff_ReturnsToken_ForDualRoleStaff()
    {
        await CreateStaffWithCredentials("both@test.com", [StaffRoleType.admin, StaffRoleType.trainer]);

        var result = await _svc.LoginStaffAsync("both@test.com", TestPassword);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginStaff_IsCaseInsensitive_ForEmail()
    {
        await CreateStaffWithCredentials("Admin@Test.COM", [StaffRoleType.admin]);

        var result = await _svc.LoginStaffAsync("ADMIN@TEST.com", TestPassword);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginStaff_ReturnsFailure_WhenPasswordWrong()
    {
        await CreateStaffWithCredentials("admin@test.com", [StaffRoleType.admin]);

        var result = await _svc.LoginStaffAsync("admin@test.com", "WrongPassword!");

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginStaff_ReturnsFailure_WhenEmailNotFound()
    {
        var result = await _svc.LoginStaffAsync("unknown@test.com", TestPassword);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginStaff_ReturnsFailure_WhenStaffIsDeleted()
    {
        var (staff, _) = await CreateStaffWithCredentials("deleted@test.com", [StaffRoleType.admin]);
        staff.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = await _svc.LoginStaffAsync("deleted@test.com", TestPassword);

        Assert.False(result.IsSuccess);
        Assert.Contains("deactivated", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Client Login ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoginClient_ReturnsToken_WhenCredentialsAreValid()
    {
        await CreateClientWithCredentials("client@test.com");

        var result = await _svc.LoginClientAsync("client@test.com", TestPassword);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
    }

    [Fact]
    public async Task LoginClient_IsCaseInsensitive_ForEmail()
    {
        await CreateClientWithCredentials("Client@Test.COM");

        var result = await _svc.LoginClientAsync("CLIENT@test.com", TestPassword);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginClient_ReturnsFailure_WhenPasswordWrong()
    {
        await CreateClientWithCredentials("client@test.com");

        var result = await _svc.LoginClientAsync("client@test.com", "WrongPass!");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginClient_ReturnsFailure_WhenEmailNotFound()
    {
        var result = await _svc.LoginClientAsync("nobody@test.com", TestPassword);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LoginClient_ReturnsFailure_WhenClientIsDeleted()
    {
        var (client, _) = await CreateClientWithCredentials("gone@test.com");
        client.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = await _svc.LoginClientAsync("gone@test.com", TestPassword);

        Assert.False(result.IsSuccess);
        Assert.Contains("deactivated", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<(Api.Data.Entities.Staff staff, StaffCredentials creds)>
        CreateStaffWithCredentials(string email, IEnumerable<StaffRoleType> roles)
    {
        var club = new Club { Name = "Test Club", Address = "Test St" };
        _db.Clubs.Add(club);

        var staff = new Api.Data.Entities.Staff
        {
            Club = club,
            FirstName = "Test", LastName = "Staff",
            Email = email.Trim().ToLower()
        };
        foreach (var role in roles)
            staff.Roles.Add(new StaffRole { Staff = staff, Role = role });

        var creds = new StaffCredentials
        {
            Staff = staff,
            Email = email.Trim().ToLower(),
            PasswordHash = TestPasswordHash,
            CreatedAt = DateTime.UtcNow
        };
        _db.Staff.Add(staff);
        _db.StaffCredentials.Add(creds);
        await _db.SaveChangesAsync();
        return (staff, creds);
    }

    private async Task<(Client client, ClientCredentials creds)>
        CreateClientWithCredentials(string email)
    {
        var client = new Client
        {
            FirstName = "Test", LastName = "Client",
            Email = email.Trim().ToLower()
        };
        var creds = new ClientCredentials
        {
            Client = client,
            Email = email.Trim().ToLower(),
            PasswordHash = TestPasswordHash,
            CreatedAt = DateTime.UtcNow
        };
        _db.Clients.Add(client);
        _db.ClientCredentials.Add(creds);
        await _db.SaveChangesAsync();
        return (client, creds);
    }
}
