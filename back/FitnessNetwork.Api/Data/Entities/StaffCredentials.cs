namespace FitnessNetwork.Api.Data.Entities;

public class StaffCredentials
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Staff Staff { get; set; } = null!;
}
