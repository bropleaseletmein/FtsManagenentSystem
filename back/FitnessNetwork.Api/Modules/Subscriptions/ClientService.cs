using FitnessNetwork.Api.Data;
using FitnessNetwork.Api.Data.Entities;
using FitnessNetwork.Common;
using Microsoft.EntityFrameworkCore;

namespace FitnessNetwork.Api.Modules.Subscriptions;

public class ClientService(AppDbContext db)
{
    public async Task<List<Client>> GetAllAsync() =>
        await db.Clients.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();

    public async Task<Client?> GetByIdAsync(Guid id) =>
        await db.Clients.Include(c => c.Subscriptions).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Result<Client>> CreateAsync(
        string firstName, string lastName, string? email, string? phone,
        DateOnly? birthDate, string? password)
    {
        var client = new Client
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            BirthDate = birthDate
        };

        if (!string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(email))
        {
            client.Credentials = new ClientCredentials
            {
                Client = client,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };
        }

        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return Result<Client>.Ok(client);
    }

    public async Task<Result> UpdateAsync(
        Guid id, string firstName, string lastName, string? email, string? phone, DateOnly? birthDate)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return Result.Fail("Client not found.");

        client.FirstName = firstName;
        client.LastName = lastName;
        client.Email = email;
        client.Phone = phone;
        client.BirthDate = birthDate;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return Result.Fail("Client not found.");

        client.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> SetCredentialsAsync(Guid clientId, string email, string password)
    {
        var client = await db.Clients.Include(c => c.Credentials).FirstOrDefaultAsync(c => c.Id == clientId);
        if (client is null) return Result.Fail("Client not found.");

        if (client.Credentials is not null)
        {
            client.Credentials.Email = email;
            client.Credentials.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        else
        {
            db.ClientCredentials.Add(new ClientCredentials
            {
                ClientId = clientId,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }
}
