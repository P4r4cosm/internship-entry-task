namespace TicTacToe.Application.DTOs;

public class MoveDto
{
    public char Player { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int MoveNumber { get; set; }
}