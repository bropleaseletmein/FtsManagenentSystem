using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FitnessNetwork.Tests.TestHelpers;

public static class DbFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
}

/// <summary>
/// Pre-built test entities inserted directly into the db to isolate the system under test.
/// </summary>
public static class Seed
{
    public static async Task<Club> ClubAsync(AppDbContext db, string name = "Test Club")
    {
        var club = new Club { Name = name, Address = "Test St 1" };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        return club;
    }

    public static async Task<Hall> HallAsync(AppDbContext db, Club club, string name = "Hall A", int capacity = 20)
    {
        var hall = new Hall { Club = club, Name = name, Capacity = capacity };
        db.Halls.Add(hall);
        await db.SaveChangesAsync();
        return hall;
    }

    public static async Task<Staff> StaffMemberAsync(
        AppDbContext db, Club club, StaffRoleType role = StaffRoleType.trainer)
    {
        var staff = new Staff
        {
            Club = club,
            FirstName = "Test",
            LastName = "Staff",
            Email = $"staff-{Guid.NewGuid():N}@test.com"
        };
        staff.Roles.Add(new StaffRole { Staff = staff, Role = role });
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    public static async Task<Client> ClientAsync(AppDbContext db, string? email = null)
    {
        var client = new Client
        {
            FirstName = "Test",
            LastName = "Client",
            Email = email ?? $"client-{Guid.NewGuid():N}@test.com"
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    public static async Task<SubscriptionType> SubscriptionTypeAsync(
        AppDbContext db,
        bool isAllClubs = true,
        int? durationDays = 30,
        int? visitsLimit = null,
        decimal price = 1000)
    {
        var st = new SubscriptionType
        {
            Name = "Test Sub",
            DurationDays = durationDays,
            VisitsLimit = visitsLimit,
            Price = price,
            IsAllClubs = isAllClubs
        };
        db.SubscriptionTypes.Add(st);
        await db.SaveChangesAsync();
        return st;
    }

    public static async Task<ClientSubscription> ActiveSubscriptionAsync(
        AppDbContext db, Client client, SubscriptionType type)
    {
        var cs = new ClientSubscription
        {
            Client = client,
            SubscriptionType = type,
            Status = SubscriptionStatus.active,
            StartedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = type.DurationDays.HasValue ? DateTime.UtcNow.AddDays(25) : null,
            VisitsLeft = type.VisitsLimit
        };
        db.ClientSubscriptions.Add(cs);
        await db.SaveChangesAsync();
        return cs;
    }

    public static async Task<ClassType> ClassTypeAsync(AppDbContext db, string name = "Yoga")
    {
        var ct = new ClassType { Name = name };
        db.ClassTypes.Add(ct);
        await db.SaveChangesAsync();
        return ct;
    }

    public static async Task<ClassSchedule> ScheduleAsync(
        AppDbContext db, ClassType classType, Hall hall, Staff trainer, int capacity = 10)
    {
        var cs = new ClassSchedule
        {
            ClassType = classType,
            Hall = hall,
            Trainer = trainer,
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = capacity,
            Status = ClassStatus.scheduled
        };
        db.ClassSchedules.Add(cs);
        await db.SaveChangesAsync();
        return cs;
    }
}
