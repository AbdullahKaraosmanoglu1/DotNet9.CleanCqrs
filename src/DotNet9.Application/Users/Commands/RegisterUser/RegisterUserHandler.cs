using DotNet9.Application.Users.Abstractions;
using DotNet9.Domain.Users;
using MediatR;

namespace DotNet9.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserHandler(IUserRepository repo) : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await repo.EmailExistsAsync(request.Email, ct))
            throw new InvalidOperationException("Email already in use");

        var user = User.Register(request.Email, request.Username);
        await repo.AddAsync(user, ct);
        return user.Id;
    }
}
