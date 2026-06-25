namespace FitnessNetwork.Api.Data.Entities;

public class Hall
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Club Club { get; set; } = null!;
    public ICollection<ClassSchedule> Classes { get; set; } = [];
}
