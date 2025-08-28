using DotNet9.Application.Users.Abstractions;
using DotNet9.Application.Users.Queries.GetUser;
using DotNet9.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNet9.Infrastructure.Users;

public sealed class UserReadService(AppDbContext db) : IUserReadService
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return u is null ? null : new UserDto(u.Id, u.Email, u.Username);
    }
}
