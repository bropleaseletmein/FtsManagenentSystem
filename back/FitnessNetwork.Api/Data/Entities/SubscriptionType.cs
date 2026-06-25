namespace FitnessNetwork.Api.Data.Entities;

public class SubscriptionType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int? DurationDays { get; set; }
    public int? VisitsLimit { get; set; }
    public decimal Price { get; set; }
    public bool IsAllClubs { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SubscriptionTypeClub> Clubs { get; set; } = [];
    public ICollection<ClientSubscription> ClientSubscriptions { get; set; } = [];
}
