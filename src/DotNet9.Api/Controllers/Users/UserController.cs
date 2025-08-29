using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DotNet9.Application.Users.Commands.RegisterUser;
using DotNet9.Application.Users.Queries.GetUser;

namespace DotNet9.Api.Controllers.Users;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Tags("Users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
    {
        var id = await _mediator.Send(cmd, ct);
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return CreatedAtAction(nameof(GetById), new { version = v, id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetUserQuery(id), ct);
        return Ok(dto);
    }
}
