namespace FitnessNetwork.Api.Data.Entities;

public class SubscriptionStatusLog
{
    public Guid Id { get; set; }
    public Guid ClientSubscriptionId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public DateTime ChangedAt { get; set; }

    public ClientSubscription ClientSubscription { get; set; } = null!;
}
