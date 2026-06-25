using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common.Jwt;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.AccessControl;

public record TurnstileResult(bool Allowed, string Message, string? ClientName = null, string? SubscriptionName = null);

public class TurnstileService(JwtService jwt, VisitService visits, AppDbContext db)
{
    public async Task<TurnstileResult> ScanAsync(string qrToken, Guid clubId, string mode)
    {
        var clientId = jwt.ValidateQrToken(qrToken);
        if (clientId is null)
            return new TurnstileResult(false, "Недействительный или просроченный QR-код");

        var client = await db.Clients.FindAsync(clientId);
        if (client is null)
            return new TurnstileResult(false, "Клиент не найден");

        var clientName = $"{client.FirstName} {client.LastName}";

        if (mode == "exit")
        {
            var activeVisit = await db.Visits
                .Where(v => v.ClientSubscription.ClientId == clientId &&
                            v.ClubId == clubId &&
                            v.ExitedAt == null)
                .FirstOrDefaultAsync();

            if (activeVisit is null)
                return new TurnstileResult(false, "Активное посещение не найдено", clientName);

            var exit = await visits.RecordExitAsync(activeVisit.Id);
            if (!exit.IsSuccess)
                return new TurnstileResult(false, exit.Error!, clientName);

            return new TurnstileResult(true, "Выход зафиксирован", clientName);
        }
        else
        {
            var sub = await db.ClientSubscriptions
                .Include(s => s.SubscriptionType).ThenInclude(st => st.Clubs)
                .Where(s => s.ClientId == clientId && s.Status == SubscriptionStatus.active)
                .Where(s => !s.ExpiresAt.HasValue || s.ExpiresAt > DateTime.UtcNow)
                .Where(s => !s.VisitsLeft.HasValue || s.VisitsLeft > 0)
                .Where(s => s.SubscriptionType.IsAllClubs ||
                            s.SubscriptionType.Clubs.Any(c => c.ClubId == clubId))
                .FirstOrDefaultAsync();

            if (sub is null)
                return new TurnstileResult(false, "Нет активного абонемента для этого клуба", clientName);

            var entry = await visits.RecordEntryAsync(clubId, sub.Id, EntryMethod.qr);
            if (!entry.IsSuccess)
                return new TurnstileResult(false, entry.Error!, clientName);

            return new TurnstileResult(true, "Доступ разрешён", clientName, sub.SubscriptionType.Name);
        }
    }
}
