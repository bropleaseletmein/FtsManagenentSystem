using FitnessNetwork.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffRole> StaffRoles => Set<StaffRole>();
    public DbSet<StaffCredentials> StaffCredentials => Set<StaffCredentials>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientCredentials> ClientCredentials => Set<ClientCredentials>();
    public DbSet<SubscriptionType> SubscriptionTypes => Set<SubscriptionType>();
    public DbSet<SubscriptionTypeClub> SubscriptionTypeClubs => Set<SubscriptionTypeClub>();
    public DbSet<ClientSubscription> ClientSubscriptions => Set<ClientSubscription>();
    public DbSet<SubscriptionFreeze> SubscriptionFreezes => Set<SubscriptionFreeze>();
    public DbSet<SubscriptionStatusLog> SubscriptionStatusLogs => Set<SubscriptionStatusLog>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<ClassBooking> ClassBookings => Set<ClassBooking>();
    public DbSet<BookingStatusLog> BookingStatusLogs => Set<BookingStatusLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Register PostgreSQL enum types (ignored by InMemory provider)
        mb.HasPostgresEnum<StaffRoleType>("public", "staff_role");
        mb.HasPostgresEnum<SubscriptionStatus>("public", "subscription_status");
        mb.HasPostgresEnum<EntryMethod>("public", "entry_method");
        mb.HasPostgresEnum<ClassStatus>("public", "class_status");
        mb.HasPostgresEnum<BookingStatus>("public", "booking_status");

        // ---- Clubs ----
        mb.Entity<Club>(e =>
        {
            e.ToTable("clubs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Address).HasColumnName("address").IsRequired();
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- Halls ----
        mb.Entity<Hall>(e =>
        {
            e.ToTable("halls");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClubId).HasColumnName("club_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasOne(x => x.Club).WithMany(c => c.Halls).HasForeignKey(x => x.ClubId);
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- Staff ----
        mb.Entity<Staff>(e =>
        {
            e.ToTable("staff");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClubId).HasColumnName("club_id");
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasOne(x => x.Club).WithMany(c => c.Staff).HasForeignKey(x => x.ClubId);
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- StaffRole ----
        mb.Entity<StaffRole>(e =>
        {
            e.ToTable("staff_roles");
            e.HasKey(x => new { x.StaffId, x.Role });
            e.Property(x => x.StaffId).HasColumnName("staff_id");
            e.Property(x => x.Role).HasColumnName("role");
            e.HasOne(x => x.Staff).WithMany(s => s.Roles).HasForeignKey(x => x.StaffId);
        });

        // ---- StaffCredentials ----
        mb.Entity<StaffCredentials>(e =>
        {
            e.ToTable("staff_credentials");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.StaffId).HasColumnName("staff_id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.HasOne(x => x.Staff).WithOne(s => s.Credentials).HasForeignKey<StaffCredentials>(x => x.StaffId);
        });

        // ---- Client ----
        mb.Entity<Client>(e =>
        {
            e.ToTable("clients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.BirthDate).HasColumnName("birth_date");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- ClientCredentials ----
        mb.Entity<ClientCredentials>(e =>
        {
            e.ToTable("client_credentials");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClientId).HasColumnName("client_id");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.HasOne(x => x.Client).WithOne(c => c.Credentials).HasForeignKey<ClientCredentials>(x => x.ClientId);
        });

        // ---- SubscriptionType ----
        mb.Entity<SubscriptionType>(e =>
        {
            e.ToTable("subscription_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.DurationDays).HasColumnName("duration_days");
            e.Property(x => x.VisitsLimit).HasColumnName("visits_limit");
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
            e.Property(x => x.IsAllClubs).HasColumnName("is_all_clubs").HasDefaultValue(false);
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- SubscriptionTypeClub ----
        mb.Entity<SubscriptionTypeClub>(e =>
        {
            e.ToTable("subscription_type_clubs");
            e.HasKey(x => new { x.SubscriptionTypeId, x.ClubId });
            e.Property(x => x.SubscriptionTypeId).HasColumnName("subscription_type_id");
            e.Property(x => x.ClubId).HasColumnName("club_id");
            e.HasOne(x => x.SubscriptionType).WithMany(st => st.Clubs).HasForeignKey(x => x.SubscriptionTypeId);
            e.HasOne(x => x.Club).WithMany().HasForeignKey(x => x.ClubId);
        });

        // ---- ClientSubscription ----
        mb.Entity<ClientSubscription>(e =>
        {
            e.ToTable("client_subscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClientId).HasColumnName("client_id");
            e.Property(x => x.SubscriptionTypeId).HasColumnName("subscription_type_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.VisitsLeft).HasColumnName("visits_left");
            e.HasOne(x => x.Client).WithMany(c => c.Subscriptions).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.SubscriptionType).WithMany(st => st.ClientSubscriptions).HasForeignKey(x => x.SubscriptionTypeId);
        });

        // ---- SubscriptionFreeze ----
        mb.Entity<SubscriptionFreeze>(e =>
        {
            e.ToTable("subscription_freezes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClientSubscriptionId).HasColumnName("client_subscription_id");
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.EndedAt).HasColumnName("ended_at");
            e.Property(x => x.DaysFrozen).HasColumnName("days_frozen");
            e.HasOne(x => x.ClientSubscription).WithMany(cs => cs.Freezes).HasForeignKey(x => x.ClientSubscriptionId);
        });

        // ---- SubscriptionStatusLog ----
        mb.Entity<SubscriptionStatusLog>(e =>
        {
            e.ToTable("subscription_status_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClientSubscriptionId).HasColumnName("client_subscription_id");
            e.Property(x => x.OldStatus).HasColumnName("old_status");
            e.Property(x => x.NewStatus).HasColumnName("new_status").IsRequired();
            e.Property(x => x.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("now()");
            e.HasOne(x => x.ClientSubscription).WithMany(cs => cs.StatusLogs).HasForeignKey(x => x.ClientSubscriptionId);
        });

        // ---- Visit ----
        mb.Entity<Visit>(e =>
        {
            e.ToTable("visits");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClubId).HasColumnName("club_id");
            e.Property(x => x.ClientSubscriptionId).HasColumnName("client_subscription_id");
            e.Property(x => x.EntryMethod).HasColumnName("entry_method");
            e.Property(x => x.EnteredAt).HasColumnName("entered_at");
            e.Property(x => x.ExitedAt).HasColumnName("exited_at");
            e.HasOne(x => x.Club).WithMany().HasForeignKey(x => x.ClubId);
            e.HasOne(x => x.ClientSubscription).WithMany(cs => cs.Visits).HasForeignKey(x => x.ClientSubscriptionId);
        });

        // ---- ClassType ----
        mb.Entity<ClassType>(e =>
        {
            e.ToTable("class_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasQueryFilter(x => x.DeletedAt == null);
        });

        // ---- ClassSchedule ----
        mb.Entity<ClassSchedule>(e =>
        {
            e.ToTable("class_schedule");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClassTypeId).HasColumnName("class_type_id");
            e.Property(x => x.HallId).HasColumnName("hall_id");
            e.Property(x => x.TrainerId).HasColumnName("trainer_id");
            e.Property(x => x.StartsAt).HasColumnName("starts_at");
            e.Property(x => x.EndsAt).HasColumnName("ends_at");
            e.Property(x => x.Capacity).HasColumnName("capacity");
            e.Property(x => x.Status).HasColumnName("status");
            e.HasOne(x => x.ClassType).WithMany(ct => ct.Classes).HasForeignKey(x => x.ClassTypeId);
            e.HasOne(x => x.Hall).WithMany(h => h.Classes).HasForeignKey(x => x.HallId);
            e.HasOne(x => x.Trainer).WithMany(s => s.Classes).HasForeignKey(x => x.TrainerId);
        });

        // ---- ClassBooking ----
        mb.Entity<ClassBooking>(e =>
        {
            e.ToTable("class_bookings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClientSubscriptionId).HasColumnName("client_subscription_id");
            e.Property(x => x.ClassScheduleId).HasColumnName("class_schedule_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.HasOne(x => x.ClientSubscription).WithMany(cs => cs.Bookings).HasForeignKey(x => x.ClientSubscriptionId);
            e.HasOne(x => x.ClassSchedule).WithMany(cs => cs.Bookings).HasForeignKey(x => x.ClassScheduleId);
        });

        // ---- BookingStatusLog ----
        mb.Entity<BookingStatusLog>(e =>
        {
            e.ToTable("booking_status_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.ClassBookingId).HasColumnName("class_booking_id");
            e.Property(x => x.OldStatus).HasColumnName("old_status");
            e.Property(x => x.NewStatus).HasColumnName("new_status").IsRequired();
            e.Property(x => x.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("now()");
            e.HasOne(x => x.ClassBooking).WithMany(cb => cb.StatusLogs).HasForeignKey(x => x.ClassBookingId);
        });
    }
}
