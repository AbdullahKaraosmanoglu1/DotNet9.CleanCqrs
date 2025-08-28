using DotNet9.Domain.Users;

namespace DotNet9.Application.Users.Abstractions;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
