namespace TicTacToe.Domain.Enums;

public enum GameStatus
{
    InProgress, // Игра продолжается
    XWins,      // Победил игрок X
    OWins,      // Победил игрок O
    Draw        // Ничья
}