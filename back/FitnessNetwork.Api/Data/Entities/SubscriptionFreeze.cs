namespace FitnessNetwork.Api.Data.Entities;

public class SubscriptionFreeze
{
    public Guid Id { get; set; }
    public Guid ClientSubscriptionId { get; set; }
    public DateOnly StartedAt { get; set; }
    public DateOnly? EndedAt { get; set; }
    public int? DaysFrozen { get; set; }

    public ClientSubscription ClientSubscription { get; set; } = null!;
}
