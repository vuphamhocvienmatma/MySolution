using Application.Users.Commands.CreateUser;
using Application.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
[EnableRateLimiting("fixed")]
public class UsersController : ControllerBase
{
    private readonly ISender _mediator;
    public UsersController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var userId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUserById), new { id = userId }, new { id = userId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.Send(query);

        return user is not null ? Ok(user) : NotFound();
    }
}