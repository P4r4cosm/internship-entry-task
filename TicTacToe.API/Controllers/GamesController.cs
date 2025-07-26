
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Game.Commands;
using TicTacToe.Application.Features.Game.Commands.MakeMove;
using TicTacToe.Application.Features.Game.Queries;

using MediatR;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using TicTacToe.Application.DTOs;

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
        Response.Headers.ETag = new StringValues($"\"{gameDto.Version}\"");
        return Ok(gameDto);
    }

    /// <summary>
    /// Сделать ход
    /// </summary>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MakeMove(Guid id, [FromBody] MakeMoveRequestDto requestDto)
    {
        
        if (!Request.Headers.TryGetValue("If-Match", out var etagValues))
        {
            return BadRequest(new { message = "If-Match header is required for making a move." });
        }
        
        // Убираем кавычки, которые добавили при отправке
        var etagString = etagValues.ToString().Trim('"');
        if (!Guid.TryParse(etagString, out var etag))
        {
            return BadRequest(new { message = "Invalid ETag format in If-Match header." });
        }
        var command = new MakeMoveCommand
        {
            GameId = id, // из URL
            ETag = etag, // из заголовка
            Player = requestDto.Player, // из тела запроса
            Row = requestDto.Row,       // из тела запроса
            Column = requestDto.Column  // из тела запроса
        };

        var gameDto = await _mediator.Send(command);
        Response.Headers.ETag = new StringValues($"\"{gameDto.Version}\"");
        return Ok(gameDto);
    }

    
    /// <summary>
    /// Проверить работоспособность
    /// </summary>
    /// <returns></returns>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok();
    }
}