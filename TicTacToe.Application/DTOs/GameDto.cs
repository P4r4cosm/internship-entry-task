using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.DTOs;

public class GameDto
{
    public Guid Id { get; set; }
    public int BoardSize { get; set; }
    public GameStatus Status { get; set; }
    public char CurrentTurn { get; set; }
    public ICollection<MoveDto> Moves { get; set; } = new List<MoveDto>();
    
    // представление доски для удобства фронтенда
    public char?[][] Board { get; set; } = null!;
}