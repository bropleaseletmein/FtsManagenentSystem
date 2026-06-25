namespace FitnessNetwork.Api.Data.Entities;

public enum StaffRoleType { admin, trainer }

public class StaffRole
{
    public Guid StaffId { get; set; }
    public StaffRoleType Role { get; set; }

    public Staff Staff { get; set; } = null!;
}
