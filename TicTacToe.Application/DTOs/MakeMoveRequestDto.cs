namespace TicTacToe.Application.DTOs;

public class MakeMoveRequestDto
{
    public required char Player { get; set; }
    public required int Row { get; set; }
    public required int Column { get; set; }
}