namespace FitnessNetwork.Api.Data.Entities;

public class BookingStatusLog
{
    public Guid Id { get; set; }
    public Guid ClassBookingId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public DateTime ChangedAt { get; set; }

    public ClassBooking ClassBooking { get; set; } = null!;
}
