namespace FitnessNetwork.Api.Data.Entities;

public enum SubscriptionStatus { pending, active, frozen, expired, cancelled }

public class ClientSubscription
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid SubscriptionTypeId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? VisitsLeft { get; set; }

    public Client Client { get; set; } = null!;
    public SubscriptionType SubscriptionType { get; set; } = null!;
    public ICollection<SubscriptionFreeze> Freezes { get; set; } = [];
    public ICollection<SubscriptionStatusLog> StatusLogs { get; set; } = [];
    public ICollection<Visit> Visits { get; set; } = [];
    public ICollection<ClassBooking> Bookings { get; set; } = [];
}
