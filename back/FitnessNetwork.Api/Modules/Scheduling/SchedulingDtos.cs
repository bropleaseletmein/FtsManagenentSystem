namespace FitnessNetwork.Api.Modules.Scheduling;

public record ScheduleItemDto(
    Guid Id,
    Guid ClassTypeId,
    string ClassTypeName,
    Guid HallId,
    string HallName,
    Guid ClubId,
    string ClubName,
    Guid TrainerId,
    string TrainerFirstName,
    string TrainerLastName,
    DateTime StartsAt,
    DateTime EndsAt,
    int Capacity,
    int BookedCount,
    string Status
);

public record BookingDto(
    Guid Id,
    Guid ClassScheduleId,
    string ClassTypeName,
    string HallName,
    string ClubName,
    string TrainerFirstName,
    string TrainerLastName,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    DateTime CreatedAt
);
