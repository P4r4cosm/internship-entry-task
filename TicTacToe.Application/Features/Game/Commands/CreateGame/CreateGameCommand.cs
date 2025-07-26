using MediatR;

namespace TicTacToe.Application.Features.Game.Commands;

public class CreateGameCommand : IRequest<Guid>
{
    public int BoardSize { get; init; }
    public int WinCondition { get; init; }
}