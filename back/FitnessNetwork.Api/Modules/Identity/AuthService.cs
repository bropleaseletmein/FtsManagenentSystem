using FitnessNetwork.Api.Data;
using FitnessNetwork.Common;
using FitnessNetwork.Common.Jwt;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Identity;

public class AuthService(AppDbContext db, JwtService jwt)
{
    public async Task<Result<string>> LoginStaffAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cred = await db.StaffCredentials
            .IgnoreQueryFilters()
            .Include(c => c.Staff).ThenInclude(s => s.Roles)
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

        if (cred is null || !BCrypt.Net.BCrypt.Verify(password, cred.PasswordHash))
            return Result<string>.Fail("Invalid email or password.");

        if (cred.Staff.DeletedAt is not null)
            return Result<string>.Fail("Account is deactivated.");

        var roles = cred.Staff.Roles.Select(r => r.Role.ToString()).ToList();
        var primaryRole = roles.Contains(Roles.Admin) ? Roles.Admin : Roles.Trainer;
        var extraRoles = roles.Where(r => r != primaryRole);

        var token = jwt.Generate(cred.StaffId, primaryRole, extraRoles);
        return Result<string>.Ok(token);
    }

    public async Task<Result<string>> LoginClientAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLower();
        var cred = await db.ClientCredentials
            .IgnoreQueryFilters()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

        if (cred is null || !BCrypt.Net.BCrypt.Verify(password, cred.PasswordHash))
            return Result<string>.Fail("Invalid email or password.");

        if (cred.Client.DeletedAt is not null)
            return Result<string>.Fail("Account is deactivated.");

        var token = jwt.Generate(cred.ClientId, Roles.Client);
        return Result<string>.Ok(token);
    }
}
