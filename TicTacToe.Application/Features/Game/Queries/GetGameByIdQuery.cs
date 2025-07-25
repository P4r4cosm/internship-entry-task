using MediatR;
using TicTacToe.Application.DTOs;

namespace TicTacToe.Application.Features.Game.Queries;

public class GetGameByIdQuery : IRequest<GameDto>
{
    public Guid Id { get; set; }
}