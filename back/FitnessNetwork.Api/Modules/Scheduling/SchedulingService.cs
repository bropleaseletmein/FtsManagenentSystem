using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Scheduling;

public class SchedulingService(AppDbContext db)
{
    // --- Class types ---

    public async Task<List<ClassType>> GetAllClassTypesAsync() =>
        await db.ClassTypes.OrderBy(ct => ct.Name).ToListAsync();

    public async Task<PagedResult<ClassType>> GetAllClassTypesPagedAsync(int page = 1, int pageSize = 20)
    {
        var query = db.ClassTypes
            .OrderBy(ct => ct.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ClassType>(items, total, page, pageSize);
    }

    public async Task<Result<ClassType>> CreateClassTypeAsync(string name, string? description)
    {
        var ct = new ClassType { Name = name, Description = description };
        db.ClassTypes.Add(ct);
        await db.SaveChangesAsync();
        return Result<ClassType>.Ok(ct);
    }

    public async Task<Result> UpdateClassTypeAsync(Guid id, string name, string? description)
    {
        var ct = await db.ClassTypes.FindAsync(id);
        if (ct is null) return Result.Fail("Class type not found.");
        ct.Name = name;
        ct.Description = description;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteClassTypeAsync(Guid id)
    {
        var ct = await db.ClassTypes.FindAsync(id);
        if (ct is null) return Result.Fail("Class type not found.");
        ct.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    // --- Schedule ---

    public async Task<List<ScheduleItemDto>> GetScheduleAsync(
        Guid? clubId, Guid? trainerId, DateTime? from, DateTime? to) =>
        await db.ClassSchedules
            .Where(cs =>
                (!clubId.HasValue || cs.Hall.ClubId == clubId) &&
                (!trainerId.HasValue || cs.TrainerId == trainerId) &&
                (!from.HasValue || cs.StartsAt >= from) &&
                (!to.HasValue || cs.StartsAt <= to))
            .OrderBy(cs => cs.StartsAt)
            .Select(cs => new ScheduleItemDto(
                cs.Id,
                cs.ClassTypeId,
                cs.ClassType.Name,
                cs.HallId,
                cs.Hall.Name,
                cs.Hall.ClubId,
                cs.Hall.Club.Name,
                cs.TrainerId,
                cs.Trainer.FirstName,
                cs.Trainer.LastName,
                cs.StartsAt,
                cs.EndsAt,
                cs.Capacity,
                cs.Bookings.Count(b => b.Status == BookingStatus.booked),
                cs.Status.ToString()
            ))
            .ToListAsync();

    public async Task<PagedResult<ScheduleItemDto>> GetSchedulePagedAsync(
        int page, int pageSize, Guid? clubId, Guid? trainerId, DateTime? from, DateTime? to)
    {
        var query = db.ClassSchedules
            .Where(cs =>
                (!clubId.HasValue || cs.Hall.ClubId == clubId) &&
                (!trainerId.HasValue || cs.TrainerId == trainerId) &&
                (!from.HasValue || cs.StartsAt >= from) &&
                (!to.HasValue || cs.StartsAt <= to))
            .OrderBy(cs => cs.StartsAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(cs => new ScheduleItemDto(
                cs.Id,
                cs.ClassTypeId,
                cs.ClassType.Name,
                cs.HallId,
                cs.Hall.Name,
                cs.Hall.ClubId,
                cs.Hall.Club.Name,
                cs.TrainerId,
                cs.Trainer.FirstName,
                cs.Trainer.LastName,
                cs.StartsAt,
                cs.EndsAt,
                cs.Capacity,
                cs.Bookings.Count(b => b.Status == BookingStatus.booked),
                cs.Status.ToString()
            ))
            .ToListAsync();

        return new PagedResult<ScheduleItemDto>(items, total, page, pageSize);
    }

    public async Task<ClassSchedule?> GetScheduleItemAsync(Guid id) =>
        await db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Hall).ThenInclude(h => h.Club)
            .Include(cs => cs.Trainer)
            .Include(cs => cs.Bookings)
            .FirstOrDefaultAsync(cs => cs.Id == id);

    public async Task<Result<ClassSchedule>> CreateScheduleAsync(
        Guid classTypeId, Guid hallId, Guid trainerId,
        DateTime startsAt, DateTime endsAt, int capacity)
    {
        var classTypeExists = await db.ClassTypes.AnyAsync(ct => ct.Id == classTypeId);
        if (!classTypeExists) return Result<ClassSchedule>.Fail("Class type not found.");

        var hallExists = await db.Halls.AnyAsync(h => h.Id == hallId);
        if (!hallExists) return Result<ClassSchedule>.Fail("Hall not found.");

        var trainerExists = await db.Staff.AnyAsync(s => s.Id == trainerId);
        if (!trainerExists) return Result<ClassSchedule>.Fail("Trainer not found.");

        var cs = new ClassSchedule
        {
            ClassTypeId = classTypeId,
            HallId = hallId,
            TrainerId = trainerId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Capacity = capacity,
            Status = ClassStatus.scheduled
        };

        db.ClassSchedules.Add(cs);
        await db.SaveChangesAsync();
        return Result<ClassSchedule>.Ok(cs);
    }

    public async Task<Result> UpdateScheduleAsync(
        Guid id, Guid classTypeId, Guid hallId, Guid trainerId,
        DateTime startsAt, DateTime endsAt, int capacity)
    {
        var cs = await db.ClassSchedules.FindAsync(id);
        if (cs is null) return Result.Fail("Schedule item not found.");
        if (cs.Status != ClassStatus.scheduled)
            return Result.Fail("Only scheduled classes can be updated.");

        cs.ClassTypeId = classTypeId;
        cs.HallId = hallId;
        cs.TrainerId = trainerId;
        cs.StartsAt = startsAt;
        cs.EndsAt = endsAt;
        cs.Capacity = capacity;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateScheduleStatusAsync(Guid id, ClassStatus newStatus)
    {
        var cs = await db.ClassSchedules.FindAsync(id);
        if (cs is null) return Result.Fail("Schedule item not found.");
        cs.Status = newStatus;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    // --- Bookings ---

    public async Task<Result<ClassBooking>> BookAsync(Guid clientSubscriptionId, Guid classScheduleId)
    {
        var sub = await db.ClientSubscriptions.FindAsync(clientSubscriptionId);
        if (sub is null) return Result<ClassBooking>.Fail("Subscription not found.");
        if (sub.Status != SubscriptionStatus.active)
            return Result<ClassBooking>.Fail("Subscription is not active.");

        var schedule = await db.ClassSchedules
            .Include(cs => cs.Bookings.Where(b => b.Status == BookingStatus.booked))
            .FirstOrDefaultAsync(cs => cs.Id == classScheduleId);

        if (schedule is null) return Result<ClassBooking>.Fail("Class not found.");
        if (schedule.Status != ClassStatus.scheduled)
            return Result<ClassBooking>.Fail("Class is not available for booking.");
        if (schedule.Bookings.Count >= schedule.Capacity)
            return Result<ClassBooking>.Fail("Class is fully booked.");

        var alreadyBooked = await db.ClassBookings.AnyAsync(
            b => b.ClientSubscriptionId == clientSubscriptionId &&
                 b.ClassScheduleId == classScheduleId &&
                 b.Status == BookingStatus.booked);
        if (alreadyBooked) return Result<ClassBooking>.Fail("Already booked.");

        var booking = new ClassBooking
        {
            ClientSubscriptionId = clientSubscriptionId,
            ClassScheduleId = classScheduleId,
            Status = BookingStatus.booked,
            CreatedAt = DateTime.UtcNow
        };

        db.ClassBookings.Add(booking);
        await db.SaveChangesAsync();
        return Result<ClassBooking>.Ok(booking);
    }

    public async Task<Result> CancelBookingAsync(Guid bookingId)
    {
        var booking = await db.ClassBookings.FindAsync(bookingId);
        if (booking is null) return Result.Fail("Booking not found.");
        if (booking.Status == BookingStatus.cancelled)
            return Result.Fail("Booking is already cancelled.");

        booking.Status = BookingStatus.cancelled;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<List<BookingDto>> GetBookingsByClientAsync(Guid clientId) =>
        await db.ClassBookings
            .Where(b => b.ClientSubscription.ClientId == clientId)
            .OrderByDescending(b => b.ClassSchedule.StartsAt)
            .Select(b => new BookingDto(
                b.Id,
                b.ClassScheduleId,
                b.ClassSchedule.ClassType.Name,
                b.ClassSchedule.Hall.Name,
                b.ClassSchedule.Hall.Club.Name,
                b.ClassSchedule.Trainer.FirstName,
                b.ClassSchedule.Trainer.LastName,
                b.ClassSchedule.StartsAt,
                b.ClassSchedule.EndsAt,
                b.Status.ToString(),
                b.CreatedAt
            ))
            .ToListAsync();
}
