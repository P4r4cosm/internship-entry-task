using FluentValidation;
using TicTacToe.Domain.Common;

namespace TicTacToe.Application.Features.Game.Commands.MakeMove;

public class MakeMoveCommandValidator : AbstractValidator<MakeMoveCommand>
{
    public MakeMoveCommandValidator()
    {
        RuleFor(v => v.GameId)
            .NotEmpty();
            
        RuleFor(v => v.Player)
            .Must(p => p == Player.X || p == Player.O)
            .WithMessage("Player must be 'X' or 'O'.");

        RuleFor(v => v.Row)
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.Column)
            .GreaterThanOrEqualTo(0);
    }
}