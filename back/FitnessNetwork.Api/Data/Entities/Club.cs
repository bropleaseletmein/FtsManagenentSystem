namespace FitnessNetwork.Api.Data.Entities;

public class Club
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Hall> Halls { get; set; } = [];
    public ICollection<Staff> Staff { get; set; } = [];
}
