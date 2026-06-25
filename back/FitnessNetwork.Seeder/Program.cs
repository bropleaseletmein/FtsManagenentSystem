using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var connStr = config.GetConnectionString("Default")!;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
dataSourceBuilder.MapEnum<StaffRoleType>("staff_role");
dataSourceBuilder.MapEnum<SubscriptionStatus>("subscription_status");
dataSourceBuilder.MapEnum<EntryMethod>("entry_method");
dataSourceBuilder.MapEnum<ClassStatus>("class_status");
dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
var dataSource = dataSourceBuilder.Build();

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(dataSource, o => o
    .MapEnum<StaffRoleType>("staff_role")
    .MapEnum<SubscriptionStatus>("subscription_status")
    .MapEnum<EntryMethod>("entry_method")
    .MapEnum<ClassStatus>("class_status")
    .MapEnum<BookingStatus>("booking_status"));

await using var db = new AppDbContext(optionsBuilder.Options);

// Check if already seeded
if (await db.Clubs.IgnoreQueryFilters().AnyAsync())
{
    Console.WriteLine("Database already contains data. Skipping seed.");
    return;
}

Console.WriteLine("Seeding database...");

// ── Clubs ──────────────────────────────────────────────────────────────────
var club1 = new Club { Name = "FitZone Central", Address = "ул. Ленина, 1", Phone = "+7-900-000-0001" };
var club2 = new Club { Name = "FitZone North", Address = "ул. Северная, 25", Phone = "+7-900-000-0002" };
var club3 = new Club { Name = "FitZone South", Address = "ул. Южная, 10", Phone = "+7-900-000-0003" };
db.Clubs.AddRange(club1, club2, club3);

// ── Halls ──────────────────────────────────────────────────────────────────
var hall1a = new Hall { Club = club1, Name = "Большой зал", Capacity = 30 };
var hall1b = new Hall { Club = club1, Name = "Зал кардио", Capacity = 20 };
var hall1c = new Hall { Club = club1, Name = "Зал единоборств", Capacity = 15 };
var hall2a = new Hall { Club = club2, Name = "Зал А", Capacity = 25 };
var hall2b = new Hall { Club = club2, Name = "Зал Б", Capacity = 20 };
var hall3a = new Hall { Club = club3, Name = "Основной зал", Capacity = 30 };
var hall3b = new Hall { Club = club3, Name = "Студия йоги", Capacity = 15 };
db.Halls.AddRange(hall1a, hall1b, hall1c, hall2a, hall2b, hall3a, hall3b);

// ── Staff ──────────────────────────────────────────────────────────────────
var staffAdmin1 = new Staff { Club = club1, FirstName = "Александр", LastName = "Петров", Email = "a.petrov@fitnessnetwork.ru" };
var staffAdmin2 = new Staff { Club = club2, FirstName = "Мария", LastName = "Иванова", Email = "m.ivanova@fitnessnetwork.ru" };
var staffTrainer1 = new Staff { Club = club1, FirstName = "Дмитрий", LastName = "Смирнов", Email = "d.smirnov@fitnessnetwork.ru" };
var staffTrainer2 = new Staff { Club = club1, FirstName = "Анна", LastName = "Козлова", Email = "a.kozlova@fitnessnetwork.ru" };
var staffTrainer3 = new Staff { Club = club2, FirstName = "Сергей", LastName = "Новиков", Email = "s.novikov@fitnessnetwork.ru" };
var staffBoth = new Staff { Club = club3, FirstName = "Олег", LastName = "Зайцев", Email = "o.zaitsev@fitnessnetwork.ru" };  // admin+trainer
var staffTrainer4 = new Staff { Club = club3, FirstName = "Наталья", LastName = "Морозова", Email = "n.morozova@fitnessnetwork.ru" };
var staffTrainer5 = new Staff { Club = club2, FirstName = "Виктор", LastName = "Волков", Email = "v.volkov@fitnessnetwork.ru" };
db.Staff.AddRange(staffAdmin1, staffAdmin2, staffTrainer1, staffTrainer2, staffTrainer3, staffBoth, staffTrainer4, staffTrainer5);

// ── Staff Roles ──────────────────────────────────────────────────────────────
db.StaffRoles.AddRange(
    new StaffRole { Staff = staffAdmin1, Role = StaffRoleType.admin },
    new StaffRole { Staff = staffAdmin2, Role = StaffRoleType.admin },
    new StaffRole { Staff = staffTrainer1, Role = StaffRoleType.trainer },
    new StaffRole { Staff = staffTrainer2, Role = StaffRoleType.trainer },
    new StaffRole { Staff = staffTrainer3, Role = StaffRoleType.trainer },
    new StaffRole { Staff = staffBoth, Role = StaffRoleType.admin },
    new StaffRole { Staff = staffBoth, Role = StaffRoleType.trainer },
    new StaffRole { Staff = staffTrainer4, Role = StaffRoleType.trainer },
    new StaffRole { Staff = staffTrainer5, Role = StaffRoleType.trainer }
);

// ── Staff Credentials ──────────────────────────────────────────────────────
db.StaffCredentials.AddRange(
    new StaffCredentials { Staff = staffAdmin1, Email = "a.petrov@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffAdmin2, Email = "m.ivanova@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffTrainer1, Email = "d.smirnov@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffTrainer2, Email = "a.kozlova@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffTrainer3, Email = "s.novikov@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffBoth, Email = "o.zaitsev@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffTrainer4, Email = "n.morozova@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"), CreatedAt = DateTime.UtcNow },
    new StaffCredentials { Staff = staffTrainer5, Email = "v.volkov@fitnessnetwork.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Trainer123!"), CreatedAt = DateTime.UtcNow }
);

// ── Subscription Types ─────────────────────────────────────────────────────
var stAllClubs = new SubscriptionType { Name = "Безлимитный (все клубы)", DurationDays = 30, Price = 5000, IsAllClubs = true };
var stSingle = new SubscriptionType { Name = "Разовое посещение", DurationDays = 1, VisitsLimit = 1, Price = 500, IsAllClubs = false };
var stMonthCentral = new SubscriptionType { Name = "Месячный (Центр)", DurationDays = 30, Price = 2500, IsAllClubs = false };
var stMonthVisits = new SubscriptionType { Name = "20 посещений (все клубы)", VisitsLimit = 20, Price = 3500, IsAllClubs = true };
db.SubscriptionTypes.AddRange(stAllClubs, stSingle, stMonthCentral, stMonthVisits);

db.SubscriptionTypeClubs.AddRange(
    new SubscriptionTypeClub { SubscriptionType = stSingle, Club = club1 },
    new SubscriptionTypeClub { SubscriptionType = stSingle, Club = club2 },
    new SubscriptionTypeClub { SubscriptionType = stSingle, Club = club3 },
    new SubscriptionTypeClub { SubscriptionType = stMonthCentral, Club = club1 }
);

// ── Clients ────────────────────────────────────────────────────────────────
var clients = new List<Client>
{
    new() { FirstName = "Иван", LastName = "Абрамов", Email = "i.abramov@mail.ru", Phone = "+79001000001", BirthDate = new DateOnly(1990, 5, 10) },
    new() { FirstName = "Елена", LastName = "Борисова", Email = "e.borisova@mail.ru", Phone = "+79001000002", BirthDate = new DateOnly(1988, 3, 22) },
    new() { FirstName = "Андрей", LastName = "Васильев", Email = "a.vasilev@mail.ru", Phone = "+79001000003", BirthDate = new DateOnly(1995, 7, 15) },
    new() { FirstName = "Ольга", LastName = "Григорьева", Email = "o.grigorieva@mail.ru", Phone = "+79001000004", BirthDate = new DateOnly(1992, 11, 30) },
    new() { FirstName = "Михаил", LastName = "Дмитриев", Email = "m.dmitriev@mail.ru", Phone = "+79001000005", BirthDate = new DateOnly(1985, 1, 5) },
    new() { FirstName = "Татьяна", LastName = "Ершова", Email = "t.ershova@mail.ru", Phone = "+79001000006", BirthDate = new DateOnly(1993, 9, 18) },
    new() { FirstName = "Николай", LastName = "Жуков", Email = "n.zhukov@mail.ru", Phone = "+79001000007", BirthDate = new DateOnly(1987, 4, 28) },
    new() { FirstName = "Светлана", LastName = "Захарова", Email = "s.zaharova@mail.ru", Phone = "+79001000008", BirthDate = new DateOnly(1994, 6, 12) },
    new() { FirstName = "Павел", LastName = "Иванов", Email = "p.ivanov@mail.ru", Phone = "+79001000009", BirthDate = new DateOnly(1991, 2, 14) },
    new() { FirstName = "Ирина", LastName = "Кузнецова", Email = "i.kuznecova@mail.ru", Phone = "+79001000010", BirthDate = new DateOnly(1989, 8, 3) },
    new() { FirstName = "Алексей", LastName = "Лебедев", Email = "a.lebedev@mail.ru", Phone = "+79001000011", BirthDate = new DateOnly(1996, 12, 25) },
    new() { FirstName = "Наталья", LastName = "Медведева", Email = "n.medvedeva@mail.ru", Phone = "+79001000012", BirthDate = new DateOnly(1990, 10, 7) },
    new() { FirstName = "Дмитрий", LastName = "Николаев", Email = "d.nikolaev@mail.ru", Phone = "+79001000013", BirthDate = new DateOnly(1983, 3, 19) },
    new() { FirstName = "Юлия", LastName = "Орлова", Email = "y.orlova@mail.ru", Phone = "+79001000014", BirthDate = new DateOnly(1997, 5, 31) },
    new() { FirstName = "Артём", LastName = "Попов", Email = "a.popov@mail.ru", Phone = "+79001000015", BirthDate = new DateOnly(1999, 7, 9) },
    new() { FirstName = "Ксения", LastName = "Романова", Email = "k.romanova@mail.ru", Phone = "+79001000016", BirthDate = new DateOnly(1986, 1, 22) },
    new() { FirstName = "Георгий", LastName = "Сидоров", Email = "g.sidorov@mail.ru", Phone = "+79001000017", BirthDate = new DateOnly(1992, 4, 14) },
    new() { FirstName = "Вера", LastName = "Тихонова", Email = "v.tihonova@mail.ru", Phone = "+79001000018", BirthDate = new DateOnly(1994, 9, 3) }
};
db.Clients.AddRange(clients);

// Client credentials
var creds = clients.Select((c, i) => new ClientCredentials
{
    Client = c,
    Email = c.Email!,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client123!"),
    CreatedAt = DateTime.UtcNow
}).ToList();
db.ClientCredentials.AddRange(creds);

// Save all basic entities first so IDs are generated
await db.SaveChangesAsync();

// ── Client Subscriptions ────────────────────────────────────────────────────
var now = DateTime.UtcNow;
var activeSubs = new List<ClientSubscription>();

// Active subscriptions
for (int i = 0; i < 10; i++)
{
    var cs = new ClientSubscription
    {
        ClientId = clients[i].Id,
        SubscriptionTypeId = stAllClubs.Id,
        Status = SubscriptionStatus.active,
        StartedAt = now.AddDays(-10),
        ExpiresAt = now.AddDays(20),
        VisitsLeft = null  // unlimited
    };
    db.ClientSubscriptions.Add(cs);
    activeSubs.Add(cs);
}

// Active limited subscription (10 visits left)
var limitedSub = new ClientSubscription
{
    ClientId = clients[10].Id,
    SubscriptionTypeId = stMonthVisits.Id,
    Status = SubscriptionStatus.active,
    StartedAt = now.AddDays(-5),
    VisitsLeft = 15
};
db.ClientSubscriptions.Add(limitedSub);
activeSubs.Add(limitedSub);

// Frozen subscription
var frozenSub = new ClientSubscription
{
    ClientId = clients[11].Id,
    SubscriptionTypeId = stMonthCentral.Id,
    Status = SubscriptionStatus.frozen,
    StartedAt = now.AddDays(-20),
    ExpiresAt = now.AddDays(10)
};
db.ClientSubscriptions.Add(frozenSub);
db.SubscriptionFreezes.Add(new SubscriptionFreeze
{
    ClientSubscription = frozenSub,
    StartedAt = DateOnly.FromDateTime(now.AddDays(-5))
});

// Expired subscription
var expiredSub = new ClientSubscription
{
    ClientId = clients[12].Id,
    SubscriptionTypeId = stAllClubs.Id,
    Status = SubscriptionStatus.expired,
    StartedAt = now.AddDays(-60),
    ExpiresAt = now.AddDays(-30)
};
db.ClientSubscriptions.Add(expiredSub);

// Cancelled subscription
var cancelledSub = new ClientSubscription
{
    ClientId = clients[13].Id,
    SubscriptionTypeId = stSingle.Id,
    Status = SubscriptionStatus.cancelled,
    StartedAt = now.AddDays(-15),
    ExpiresAt = now.AddDays(-14),
    VisitsLeft = 0
};
db.ClientSubscriptions.Add(cancelledSub);

await db.SaveChangesAsync();

// ── Class Types ────────────────────────────────────────────────────────────
var ctYoga = new ClassType { Name = "Йога", Description = "Расслабление и растяжка" };
var ctBoxing = new ClassType { Name = "Бокс", Description = "Единоборства для всех уровней" };
var ctSpinning = new ClassType { Name = "Спиннинг", Description = "Велотренировка высокой интенсивности" };
var ctPilates = new ClassType { Name = "Пилатес", Description = "Укрепление мышц и осанки" };
db.ClassTypes.AddRange(ctYoga, ctBoxing, ctSpinning, ctPilates);
await db.SaveChangesAsync();

// ── Class Schedule (next week) ──────────────────────────────────────────────
var monday = now.Date.AddDays(((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7 + 1);

var schedules = new List<ClassSchedule>
{
    new() { ClassTypeId = ctYoga.Id, HallId = hall1a.Id, TrainerId = staffTrainer2.Id, StartsAt = monday.AddHours(9), EndsAt = monday.AddHours(10), Capacity = 20 },
    new() { ClassTypeId = ctSpinning.Id, HallId = hall1b.Id, TrainerId = staffTrainer1.Id, StartsAt = monday.AddHours(11), EndsAt = monday.AddHours(12), Capacity = 15 },
    new() { ClassTypeId = ctBoxing.Id, HallId = hall1c.Id, TrainerId = staffTrainer1.Id, StartsAt = monday.AddHours(18), EndsAt = monday.AddHours(19), Capacity = 12 },
    new() { ClassTypeId = ctPilates.Id, HallId = hall2a.Id, TrainerId = staffTrainer3.Id, StartsAt = monday.AddDays(1).AddHours(10), EndsAt = monday.AddDays(1).AddHours(11), Capacity = 18 },
    new() { ClassTypeId = ctYoga.Id, HallId = hall3b.Id, TrainerId = staffTrainer4.Id, StartsAt = monday.AddDays(1).AddHours(9), EndsAt = monday.AddDays(1).AddHours(10), Capacity = 12 },
    new() { ClassTypeId = ctSpinning.Id, HallId = hall2b.Id, TrainerId = staffTrainer5.Id, StartsAt = monday.AddDays(2).AddHours(18), EndsAt = monday.AddDays(2).AddHours(19), Capacity = 18 },
    new() { ClassTypeId = ctBoxing.Id, HallId = hall1c.Id, TrainerId = staffBoth.Id, StartsAt = monday.AddDays(3).AddHours(19), EndsAt = monday.AddDays(3).AddHours(20), Capacity = 15 },
    new() { ClassTypeId = ctPilates.Id, HallId = hall3a.Id, TrainerId = staffTrainer4.Id, StartsAt = monday.AddDays(4).AddHours(10), EndsAt = monday.AddDays(4).AddHours(11), Capacity = 20 }
};
db.ClassSchedules.AddRange(schedules);
await db.SaveChangesAsync();

// ── Bookings ────────────────────────────────────────────────────────────────
db.ClassBookings.AddRange(
    new ClassBooking { ClientSubscriptionId = activeSubs[0].Id, ClassScheduleId = schedules[0].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[1].Id, ClassScheduleId = schedules[0].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[2].Id, ClassScheduleId = schedules[0].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[3].Id, ClassScheduleId = schedules[1].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[4].Id, ClassScheduleId = schedules[1].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[0].Id, ClassScheduleId = schedules[2].Id, Status = BookingStatus.cancelled, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[5].Id, ClassScheduleId = schedules[3].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = activeSubs[6].Id, ClassScheduleId = schedules[4].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow },
    new ClassBooking { ClientSubscriptionId = limitedSub.Id, ClassScheduleId = schedules[0].Id, Status = BookingStatus.booked, CreatedAt = DateTime.UtcNow }
);

// ── Visits ────────────────────────────────────────────────────────────────
// Past visits (with exit recorded)
db.Visits.AddRange(
    new Visit { ClubId = club1.Id, ClientSubscriptionId = activeSubs[0].Id, EntryMethod = EntryMethod.card, EnteredAt = now.AddDays(-3).AddHours(9), ExitedAt = now.AddDays(-3).AddHours(11) },
    new Visit { ClubId = club1.Id, ClientSubscriptionId = activeSubs[1].Id, EntryMethod = EntryMethod.qr, EnteredAt = now.AddDays(-3).AddHours(10), ExitedAt = now.AddDays(-3).AddHours(12) },
    new Visit { ClubId = club2.Id, ClientSubscriptionId = activeSubs[2].Id, EntryMethod = EntryMethod.bracelet, EnteredAt = now.AddDays(-2).AddHours(8), ExitedAt = now.AddDays(-2).AddHours(10) },
    new Visit { ClubId = club1.Id, ClientSubscriptionId = activeSubs[3].Id, EntryMethod = EntryMethod.card, EnteredAt = now.AddDays(-1).AddHours(17), ExitedAt = now.AddDays(-1).AddHours(19) },
    new Visit { ClubId = club3.Id, ClientSubscriptionId = activeSubs[4].Id, EntryMethod = EntryMethod.qr, EnteredAt = now.AddDays(-1).AddHours(9), ExitedAt = now.AddDays(-1).AddHours(10) },
    // Current visits (still inside)
    new Visit { ClubId = club1.Id, ClientSubscriptionId = activeSubs[5].Id, EntryMethod = EntryMethod.card, EnteredAt = now.AddHours(-1) },
    new Visit { ClubId = club2.Id, ClientSubscriptionId = activeSubs[6].Id, EntryMethod = EntryMethod.bracelet, EnteredAt = now.AddMinutes(-45) }
);

await db.SaveChangesAsync();

Console.WriteLine("Seed complete.");
Console.WriteLine($"  Clubs: 3, Halls: 7");
Console.WriteLine($"  Staff: 8 (2 admins, 5 trainers, 1 admin+trainer)");
Console.WriteLine($"  Clients: {clients.Count}");
Console.WriteLine($"  Subscription types: 4");
Console.WriteLine($"  Class types: 4, Schedule: {schedules.Count} classes");
Console.WriteLine($"  Bookings: 9, Visits: 7");
Console.WriteLine();
Console.WriteLine("Staff login examples:");
Console.WriteLine("  Admin: a.petrov@fitnessnetwork.ru / Admin123!");
Console.WriteLine("  Trainer: d.smirnov@fitnessnetwork.ru / Trainer123!");
Console.WriteLine("Client login example:");
Console.WriteLine("  i.abramov@mail.ru / Client123!");
