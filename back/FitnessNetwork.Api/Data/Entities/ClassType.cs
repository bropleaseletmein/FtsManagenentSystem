namespace FitnessNetwork.Api.Data.Entities;

public class ClassType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ClassSchedule> Classes { get; set; } = [];
}
