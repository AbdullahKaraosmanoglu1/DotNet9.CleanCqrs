using MediatR;

namespace DotNet9.Application.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid Id) : IRequest<UserDto>;
public sealed record UserDto(Guid Id, string Email, string Username);
