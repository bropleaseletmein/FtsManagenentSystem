using Dapper;
using FitnessNetwork.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;
using System.Text.Json;

namespace FitnessNetwork.Api.Modules.Reporting;

class AttendanceRow { public Guid ClubId { get; set; } public DateTime Date { get; set; } public long VisitCount { get; set; } }
class WorkloadRow { public Guid TrainerId { get; set; } public string TrainerName { get; set; } public long TotalClasses { get; set; } public long TotalBookings { get; set; } }
class OccupancyRow { public Guid Id { get; set; } public string ClassTypeName { get; set; } public DateTime StartsAt { get; set; } public int Capacity { get; set; } public long ActiveBookings { get; set; } public decimal OccupancyPct { get; set; } }
class CurrentRow { public Guid ClubId { get; set; } public string ClubName { get; set; } public long CurrentVisitors { get; set; } }

public class ReportingService(AppDbContext db, IDistributedCache cache)
{
    async Task<IDbConnection> Conn()
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync();
        return conn;
    }

    async Task<T> GetOrSet<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var cached = await cache.GetStringAsync(key);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached, opts)!;

        var value = await factory();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value, opts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        return value;
    }

    public Task<object> GetAttendanceAsync(Guid? clubId, DateTime? from, DateTime? to)
    {
        var key = $"attendance:{clubId}:{from:yyyyMMdd}:{to:yyyyMMdd}";
        return GetOrSet<object>(key, TimeSpan.FromMinutes(5), async () =>
        {
            var cond = new List<string>();
            var p = new DynamicParameters();
            if (clubId.HasValue) { cond.Add("v.club_id = @clubId"); p.Add("clubId", clubId.Value); }
            if (from.HasValue)   { cond.Add("v.entered_at >= @from"); p.Add("from", from.Value); }
            if (to.HasValue)     { cond.Add("v.entered_at <= @to");   p.Add("to",   to.Value); }
            var where = cond.Count > 0 ? "WHERE " + string.Join(" AND ", cond) : "";
            var sql = $"""
                SELECT
                    v.club_id            AS ClubId,
                    v.entered_at::date   AS Date,
                    COUNT(*)             AS VisitCount
                FROM visits v
                {where}
                GROUP BY v.club_id, v.entered_at::date
                ORDER BY v.entered_at::date
                """;
            return (object)(await (await Conn()).QueryAsync<AttendanceRow>(sql, p)).ToList();
        });
    }

    public Task<object> GetTrainerWorkloadAsync(Guid? clubId, DateTime? from, DateTime? to)
    {
        var key = $"workload:{clubId}:{from:yyyyMMdd}:{to:yyyyMMdd}";
        return GetOrSet<object>(key, TimeSpan.FromMinutes(5), async () =>
        {
            var cond = new List<string> { "s.deleted_at IS NULL" };
            var p = new DynamicParameters();
            if (clubId.HasValue) { cond.Add("h.club_id = @clubId"); p.Add("clubId", clubId.Value); }
            if (from.HasValue)   { cond.Add("cs.starts_at >= @from"); p.Add("from", from.Value); }
            if (to.HasValue)     { cond.Add("cs.starts_at <= @to");   p.Add("to",   to.Value); }
            var where = "WHERE " + string.Join(" AND ", cond);
            var sql = $"""
                SELECT
                    s.id                                AS TrainerId,
                    s.first_name || ' ' || s.last_name  AS TrainerName,
                    COUNT(DISTINCT cs.id)               AS TotalClasses,
                    COUNT(b.id)                         AS TotalBookings
                FROM class_schedule cs
                JOIN staff s ON s.id = cs.trainer_id
                JOIN halls h ON h.id = cs.hall_id
                LEFT JOIN class_bookings b
                       ON b.class_schedule_id = cs.id AND b.status = 'booked'
                {where}
                GROUP BY s.id, s.first_name, s.last_name
                ORDER BY TotalClasses DESC
                """;
            return (object)(await (await Conn()).QueryAsync<WorkloadRow>(sql, p)).ToList();
        });
    }

    public Task<object> GetClassOccupancyAsync(DateTime? from, DateTime? to)
    {
        var key = $"classes:{from:yyyyMMdd}:{to:yyyyMMdd}";
        return GetOrSet<object>(key, TimeSpan.FromMinutes(5), async () =>
        {
            var cond = new List<string> { "ct.deleted_at IS NULL" };
            var p = new DynamicParameters();
            if (from.HasValue) { cond.Add("cs.starts_at >= @from"); p.Add("from", from.Value); }
            if (to.HasValue)   { cond.Add("cs.starts_at <= @to");   p.Add("to",   to.Value); }
            var where = "WHERE " + string.Join(" AND ", cond);
            var sql = $"""
                SELECT
                    cs.id                               AS Id,
                    ct.name                             AS ClassTypeName,
                    cs.starts_at                        AS StartsAt,
                    cs.capacity                         AS Capacity,
                    COUNT(b.id)                         AS ActiveBookings,
                    CASE WHEN cs.capacity = 0 THEN 0.0
                         ELSE ROUND(COUNT(b.id) * 100.0 / cs.capacity, 1)
                    END                                 AS OccupancyPct
                FROM class_schedule cs
                JOIN class_types ct ON ct.id = cs.class_type_id
                LEFT JOIN class_bookings b
                       ON b.class_schedule_id = cs.id AND b.status = 'booked'
                {where}
                GROUP BY cs.id, ct.name, cs.starts_at, cs.capacity
                ORDER BY cs.starts_at DESC
                """;
            return (object)(await (await Conn()).QueryAsync<OccupancyRow>(sql, p)).ToList();
        });
    }

    public Task<object> GetCurrentOccupancyAsync()
    {
        return GetOrSet<object>("current-occupancy", TimeSpan.FromMinutes(1), async () =>
        {
            const string sql = """
                SELECT
                    v.club_id   AS ClubId,
                    c.name      AS ClubName,
                    COUNT(*)    AS CurrentVisitors
                FROM visits v
                JOIN clubs c ON c.id = v.club_id AND c.deleted_at IS NULL
                WHERE v.exited_at IS NULL
                GROUP BY v.club_id, c.name
                ORDER BY c.name
                """;
            return (object)(await (await Conn()).QueryAsync<CurrentRow>(sql)).ToList();
        });
    }
}
