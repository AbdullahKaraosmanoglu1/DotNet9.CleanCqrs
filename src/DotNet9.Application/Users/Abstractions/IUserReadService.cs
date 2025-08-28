using DotNet9.Application.Users.Queries.GetUser;

namespace DotNet9.Application.Users.Abstractions;

public interface IUserReadService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
}
