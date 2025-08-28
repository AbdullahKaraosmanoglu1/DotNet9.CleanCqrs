using DotNet9.Application.Users.Abstractions;
using MediatR;

namespace DotNet9.Application.Users.Queries.GetUser;

public sealed class GetUserHandler(IUserReadService read) : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
        => await read.GetByIdAsync(request.Id, ct)
           ?? throw new KeyNotFoundException("User not found");
}
