namespace FitnessNetwork.Api.Data.Entities;

public enum BookingStatus { booked, cancelled }

public class ClassBooking
{
    public Guid Id { get; set; }
    public Guid ClientSubscriptionId { get; set; }
    public Guid ClassScheduleId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ClientSubscription ClientSubscription { get; set; } = null!;
    public ClassSchedule ClassSchedule { get; set; } = null!;
    public ICollection<BookingStatusLog> StatusLogs { get; set; } = [];
}
