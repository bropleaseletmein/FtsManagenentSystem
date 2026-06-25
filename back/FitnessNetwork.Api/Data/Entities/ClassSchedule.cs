namespace FitnessNetwork.Api.Data.Entities;

public enum ClassStatus { scheduled, cancelled, completed }

public class ClassSchedule
{
    public Guid Id { get; set; }
    public Guid ClassTypeId { get; set; }
    public Guid HallId { get; set; }
    public Guid TrainerId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int Capacity { get; set; }
    public ClassStatus Status { get; set; }

    public ClassType ClassType { get; set; } = null!;
    public Hall Hall { get; set; } = null!;
    public Staff Trainer { get; set; } = null!;
    public ICollection<ClassBooking> Bookings { get; set; } = [];
}
