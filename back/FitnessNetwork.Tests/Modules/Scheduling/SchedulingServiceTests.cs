using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Api.Modules.Scheduling;
using FitnessNetwork.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitnessNetwork.Tests.Modules.Scheduling;

public class SchedulingServiceTests : IDisposable
{
    private readonly Api.Data.AppDbContext _db;
    private readonly SchedulingService _svc;

    public SchedulingServiceTests()
    {
        _db = DbFactory.Create();
        _svc = new SchedulingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── ClassTypes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllClassTypes_ReturnsOnlyNonDeleted()
    {
        await Seed.ClassTypeAsync(_db, "Yoga");
        _db.ClassTypes.Add(new ClassType { Name = "Deleted", DeletedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _svc.GetAllClassTypesAsync();

        Assert.Single(result);
        Assert.Equal("Yoga", result[0].Name);
    }

    [Fact]
    public async Task CreateClassType_CreatesSuccessfully()
    {
        var result = await _svc.CreateClassTypeAsync("Pilates", "Core strength training");

        Assert.True(result.IsSuccess);
        Assert.Equal("Pilates", result.Value!.Name);
        Assert.Equal("Core strength training", result.Value.Description);
    }

    [Fact]
    public async Task DeleteClassType_SetsSoftDelete()
    {
        var ct = await Seed.ClassTypeAsync(_db);

        var result = await _svc.DeleteClassTypeAsync(ct.Id);

        Assert.True(result.IsSuccess);
        var raw = await _db.ClassTypes.IgnoreQueryFilters().FirstAsync(x => x.Id == ct.Id);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task DeleteClassType_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.DeleteClassTypeAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
    }

    // ── Schedule ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSchedule_CreatesSuccessfully()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club, StaffRoleType.trainer);
        var ct = await Seed.ClassTypeAsync(_db);

        var starts = DateTime.UtcNow.AddDays(1);
        var ends = starts.AddHours(1);
        var result = await _svc.CreateScheduleAsync(ct.Id, hall.Id, trainer.Id, starts, ends, 15);

        Assert.True(result.IsSuccess);
        var cs = result.Value!;
        Assert.Equal(ClassStatus.scheduled, cs.Status);
        Assert.Equal(15, cs.Capacity);
    }

    [Fact]
    public async Task CreateSchedule_ReturnsFailure_WhenClassTypeNotFound()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var starts = DateTime.UtcNow.AddDays(1);

        var result = await _svc.CreateScheduleAsync(
            Guid.NewGuid(), hall.Id, trainer.Id, starts, starts.AddHours(1), 10);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateSchedule_ReturnsFailure_WhenHallNotFound()
    {
        var club = await Seed.ClubAsync(_db);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var starts = DateTime.UtcNow.AddDays(1);

        var result = await _svc.CreateScheduleAsync(
            ct.Id, Guid.NewGuid(), trainer.Id, starts, starts.AddHours(1), 10);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateScheduleStatus_ChangesStatus()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer);

        var result = await _svc.UpdateScheduleStatusAsync(schedule.Id, ClassStatus.cancelled);

        Assert.True(result.IsSuccess);
        var updated = await _db.ClassSchedules.FindAsync(schedule.Id);
        Assert.Equal(ClassStatus.cancelled, updated!.Status);
    }

    [Fact]
    public async Task GetSchedule_FiltersByTrainer()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer1 = await Seed.StaffMemberAsync(_db, club);
        var trainer2 = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);

        await Seed.ScheduleAsync(_db, ct, hall, trainer1);
        await Seed.ScheduleAsync(_db, ct, hall, trainer2);

        var result = await _svc.GetScheduleAsync(null, trainer1.Id, null, null);

        Assert.Single(result);
        Assert.Equal(trainer1.Id, result[0].TrainerId);
    }

    // ── Bookings ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BookAsync_CreatesBooking()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club, capacity: 20);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 20);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.BookAsync(sub.Id, schedule.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.booked, result.Value!.Status);
        Assert.Equal(sub.Id, result.Value.ClientSubscriptionId);
    }

    [Fact]
    public async Task BookAsync_ReturnsFailure_WhenClassFull()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 1);

        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);

        var client1 = await Seed.ClientAsync(_db);
        var sub1 = await Seed.ActiveSubscriptionAsync(_db, client1, type);
        await _svc.BookAsync(sub1.Id, schedule.Id);

        var client2 = await Seed.ClientAsync(_db);
        var sub2 = await Seed.ActiveSubscriptionAsync(_db, client2, type);
        var result = await _svc.BookAsync(sub2.Id, schedule.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("fully booked", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BookAsync_ReturnsFailure_WhenSubscriptionNotActive()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 10);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db);
        var sub = new ClientSubscription
        {
            Client = client, SubscriptionType = type,
            Status = SubscriptionStatus.expired,
            StartedAt = DateTime.UtcNow.AddDays(-60)
        };
        _db.ClientSubscriptions.Add(sub);
        await _db.SaveChangesAsync();

        var result = await _svc.BookAsync(sub.Id, schedule.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BookAsync_ReturnsFailure_WhenAlreadyBooked()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 10);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        await _svc.BookAsync(sub.Id, schedule.Id);
        var result = await _svc.BookAsync(sub.Id, schedule.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("already booked", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BookAsync_ReturnsFailure_WhenClassCancelled()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 10);
        await _svc.UpdateScheduleStatusAsync(schedule.Id, ClassStatus.cancelled);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);

        var result = await _svc.BookAsync(sub.Id, schedule.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("not available", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelBookingAsync_CancelsBooking()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 10);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var bookResult = await _svc.BookAsync(sub.Id, schedule.Id);

        var result = await _svc.CancelBookingAsync(bookResult.Value!.Id);

        Assert.True(result.IsSuccess);
        var updated = await _db.ClassBookings.FindAsync(bookResult.Value.Id);
        Assert.Equal(BookingStatus.cancelled, updated!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsFailure_WhenAlreadyCancelled()
    {
        var club = await Seed.ClubAsync(_db);
        var hall = await Seed.HallAsync(_db, club);
        var trainer = await Seed.StaffMemberAsync(_db, club);
        var ct = await Seed.ClassTypeAsync(_db);
        var schedule = await Seed.ScheduleAsync(_db, ct, hall, trainer, capacity: 10);

        var client = await Seed.ClientAsync(_db);
        var type = await Seed.SubscriptionTypeAsync(_db, isAllClubs: true);
        var sub = await Seed.ActiveSubscriptionAsync(_db, client, type);
        var bookResult = await _svc.BookAsync(sub.Id, schedule.Id);

        await _svc.CancelBookingAsync(bookResult.Value!.Id);
        var result = await _svc.CancelBookingAsync(bookResult.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("already cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsFailure_WhenNotFound()
    {
        var result = await _svc.CancelBookingAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
    }
}
