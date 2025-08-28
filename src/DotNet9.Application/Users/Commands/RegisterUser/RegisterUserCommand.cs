using MediatR;

namespace DotNet9.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Username) : IRequest<Guid>;
