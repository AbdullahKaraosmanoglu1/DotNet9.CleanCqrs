using DotNet9.Application.Users.Abstractions;
using DotNet9.Domain.Users;
using DotNet9.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNet9.Infrastructure.Users;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }
}
