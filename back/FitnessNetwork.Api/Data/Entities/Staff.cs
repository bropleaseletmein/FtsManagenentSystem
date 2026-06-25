namespace FitnessNetwork.Api.Data.Entities;

public class Staff
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime? DeletedAt { get; set; }

    public Club Club { get; set; } = null!;
    public ICollection<StaffRole> Roles { get; set; } = [];
    public StaffCredentials? Credentials { get; set; }
    public ICollection<ClassSchedule> Classes { get; set; } = [];
}
