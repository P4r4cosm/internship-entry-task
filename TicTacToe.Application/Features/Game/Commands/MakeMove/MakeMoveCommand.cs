using System.Text.Json.Serialization;
using MediatR;
using TicTacToe.Application.DTOs;

namespace TicTacToe.Application.Features.Game.Commands.MakeMove;

public class MakeMoveCommand : IRequest<GameDto>
{
    // JsonIgnore, т.к. GameId будет браться из URL, а не из тела запроса
    [JsonIgnore]
    public Guid GameId { get; set; } 
    
    public char Player { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
}