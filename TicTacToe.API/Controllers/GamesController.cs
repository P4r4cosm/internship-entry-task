using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Game.Commands;
using TicTacToe.Application.Features.Game.Commands.MakeMove;
using TicTacToe.Application.Features.Game.Queries;

using MediatR;

namespace TicTacToe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly ISender _mediator; 

    public GamesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Создаёт новую игру
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGame()
    {
        var gameId = await _mediator.Send(new CreateGameCommand());

        // Возвращаем 201 Created с заголовком Location, как требует REST
        return CreatedAtAction(nameof(GetGame), new { id = gameId }, new { gameId });
    }

    /// <summary>
    /// Получает игру
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(Guid id)
    {
        var gameDto = await _mediator.Send(new GetGameByIdQuery { Id = id });
        return Ok(gameDto);
    }

    /// <summary>
    /// Сделать ход
    /// </summary>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MakeMove(Guid id, [FromBody] MakeMoveCommand command)
    {
        // Присваиваем ID из URL'а команде перед отправкой
        command.GameId = id;

        var gameDto = await _mediator.Send(command);
        return Ok(gameDto);
    }
}