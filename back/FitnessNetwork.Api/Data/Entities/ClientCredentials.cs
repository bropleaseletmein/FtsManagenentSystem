namespace FitnessNetwork.Api.Data.Entities;

public class ClientCredentials
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Client Client { get; set; } = null!;
}
