using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Staff;

public class StaffService(AppDbContext db)
{
    public async Task<List<Data.Entities.Staff>> GetAllAsync() =>
        await db.Staff.Include(s => s.Roles).Include(s => s.Club).OrderBy(s => s.LastName).ToListAsync();

    public async Task<Data.Entities.Staff?> GetByIdAsync(Guid id) =>
        await db.Staff.Include(s => s.Roles).Include(s => s.Club).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Result<Data.Entities.Staff>> CreateAsync(
        Guid clubId, string firstName, string lastName, string email,
        IEnumerable<StaffRoleType> roles, string? password)
    {
        var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId);
        if (!clubExists) return Result<Data.Entities.Staff>.Fail("Club not found.");

        var staff = new Data.Entities.Staff
        {
            ClubId = clubId,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };

        foreach (var role in roles.Distinct())
            staff.Roles.Add(new StaffRole { Staff = staff, Role = role });

        if (!string.IsNullOrWhiteSpace(password))
        {
            staff.Credentials = new StaffCredentials
            {
                Staff = staff,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };
        }

        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return Result<Data.Entities.Staff>.Ok(staff);
    }

    public async Task<Result> UpdateAsync(Guid id, string firstName, string lastName, string email)
    {
        var staff = await db.Staff.FindAsync(id);
        if (staff is null) return Result.Fail("Staff not found.");

        staff.FirstName = firstName;
        staff.LastName = lastName;
        staff.Email = email;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var staff = await db.Staff.FindAsync(id);
        if (staff is null) return Result.Fail("Staff not found.");

        staff.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> AddRoleAsync(Guid staffId, StaffRoleType role)
    {
        var staff = await db.Staff.Include(s => s.Roles).FirstOrDefaultAsync(s => s.Id == staffId);
        if (staff is null) return Result.Fail("Staff not found.");

        if (staff.Roles.Any(r => r.Role == role)) return Result.Fail("Role already assigned.");

        db.StaffRoles.Add(new StaffRole { StaffId = staffId, Role = role });
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> RemoveRoleAsync(Guid staffId, StaffRoleType role)
    {
        var existing = await db.StaffRoles.FindAsync(staffId, role);
        if (existing is null) return Result.Fail("Role not found on this staff member.");

        db.StaffRoles.Remove(existing);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> SetCredentialsAsync(Guid staffId, string email, string password)
    {
        var staff = await db.Staff.Include(s => s.Credentials).FirstOrDefaultAsync(s => s.Id == staffId);
        if (staff is null) return Result.Fail("Staff not found.");

        if (staff.Credentials is not null)
        {
            staff.Credentials.Email = email;
            staff.Credentials.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        else
        {
            db.StaffCredentials.Add(new StaffCredentials
            {
                StaffId = staffId,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }
}
