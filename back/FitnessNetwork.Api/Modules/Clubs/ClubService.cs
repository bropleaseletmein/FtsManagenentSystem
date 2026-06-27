using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Clubs;

public class ClubService(AppDbContext db)
{
    // --- Clubs ---

    public async Task<List<Club>> GetAllClubsAsync() =>
        await db.Clubs.OrderBy(c => c.Name).ToListAsync();

    public async Task<PagedResult<Club>> GetAllClubsPagedAsync(int page = 1, int pageSize = 20)
    {
        var query = db.Clubs
            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Club>(items, total, page, pageSize);
    }

    public async Task<Club?> GetClubByIdAsync(Guid id) =>
        await db.Clubs.Include(c => c.Halls).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Club> CreateClubAsync(string name, string address, string? phone)
    {
        var club = new Club { Name = name, Address = address, Phone = phone };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        return club;
    }

    public async Task<Result> UpdateClubAsync(Guid id, string name, string address, string? phone)
    {
        var club = await db.Clubs.FindAsync(id);
        if (club is null) return Result.Fail("Club not found.");

        club.Name = name;
        club.Address = address;
        club.Phone = phone;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteClubAsync(Guid id)
    {
        var club = await db.Clubs.FindAsync(id);
        if (club is null) return Result.Fail("Club not found.");

        club.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    // --- Halls ---

    public async Task<List<Hall>> GetHallsByClubAsync(Guid clubId) =>
        await db.Halls.Where(h => h.ClubId == clubId).OrderBy(h => h.Name).ToListAsync();

    public async Task<Hall?> GetHallByIdAsync(Guid id) =>
        await db.Halls.FindAsync(id);

    public async Task<Result<Hall>> CreateHallAsync(Guid clubId, string name, int capacity)
    {
        var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId);
        if (!clubExists) return Result<Hall>.Fail("Club not found.");

        var hall = new Hall { ClubId = clubId, Name = name, Capacity = capacity };
        db.Halls.Add(hall);
        await db.SaveChangesAsync();
        return Result<Hall>.Ok(hall);
    }

    public async Task<Result> UpdateHallAsync(Guid id, string name, int capacity)
    {
        var hall = await db.Halls.FindAsync(id);
        if (hall is null) return Result.Fail("Hall not found.");

        hall.Name = name;
        hall.Capacity = capacity;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteHallAsync(Guid id)
    {
        var hall = await db.Halls.FindAsync(id);
        if (hall is null) return Result.Fail("Hall not found.");

        hall.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }
}
