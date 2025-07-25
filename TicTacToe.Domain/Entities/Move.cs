namespace TicTacToe.Domain.Entities;

public class Move
{
   
    public long Id { get; private set; }
    public Guid GameId { get; private set; }

    public char Player { get; private set; } // Игрок, который фактически появился на доске
    public int Row { get; private set; }
    public int Column { get; private set; }
    public int MoveNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Приватный конструктор для EF Core
    private Move() { }

    // Публичный конструктор для создания хода
    public Move(Guid gameId, char player, int row, int column, int moveNumber)
    {
        GameId = gameId;
        Player = player;
        Row = row;
        Column = column;
        MoveNumber = moveNumber;
        CreatedAt = DateTime.UtcNow;
    }
}