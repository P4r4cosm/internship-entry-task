namespace TicTacToe.Domain.Common;

public static class Player
{
    public const char X = 'X';
    public const char O = 'O';

    public static char GetOpponent(char player)
    {
        if (player == X) return O;
        if (player == O) return X;
        
        throw new ArgumentException("Unknown player", nameof(player));
    }
}