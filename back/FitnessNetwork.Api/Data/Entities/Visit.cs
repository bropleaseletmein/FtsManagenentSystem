namespace FitnessNetwork.Api.Data.Entities;

public enum EntryMethod { card, qr, bracelet }

public class Visit
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid ClientSubscriptionId { get; set; }
    public EntryMethod EntryMethod { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }

    public Club Club { get; set; } = null!;
    public ClientSubscription ClientSubscription { get; set; } = null!;
}
