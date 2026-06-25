namespace FitnessNetwork.Api.Data.Entities;

public class SubscriptionTypeClub
{
    public Guid SubscriptionTypeId { get; set; }
    public Guid ClubId { get; set; }

    public SubscriptionType SubscriptionType { get; set; } = null!;
    public Club Club { get; set; } = null!;
}
