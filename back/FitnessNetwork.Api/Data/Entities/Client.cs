namespace FitnessNetwork.Api.Data.Entities;

public class Client
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ClientCredentials? Credentials { get; set; }
    public ICollection<ClientSubscription> Subscriptions { get; set; } = [];
}
