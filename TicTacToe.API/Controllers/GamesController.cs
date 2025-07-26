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
    private readonly IConfiguration _configuration;

    public GamesController(ISender mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// Создаёт новую игру
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGame()
    {
        // 1. Читаем правила игры из конфигурации
        var boardSizeSection = _configuration.GetSection("BoardSize");
        var winConditionSection = _configuration.GetSection("WinCondition");

        var boardSize = int.Parse(boardSizeSection.Value);
        var winCondition = int.Parse(winConditionSection.Value);

        var gameId = await _mediator.Send(
            new CreateGameCommand { BoardSize = boardSize, WinCondition = winCondition });

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
    public async Task<IActionResult> MakeMove(Guid id, [FromBody] MakeMoveRequestDto requestDto, [FromHeader(Name = "If-Match")] string etag)
    {
        var etagString = etag.Trim('"');
        if (!Guid.TryParse(etagString, out var etagGuid))
        {
            // Статус 412 Precondition Failed более корректен для неверного ETag
            return StatusCode(StatusCodes.Status412PreconditionFailed, new { message = "Invalid ETag format in If-Match header." });
        }

        var command = new MakeMoveCommand
        {
            GameId = id,
            ETag = etagGuid, // Используем распарсенный Guid
            Player = requestDto.Player,
            Row = requestDto.Row,
            Column = requestDto.Column
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