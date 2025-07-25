namespace TicTacToe.Application.Common.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string exMessage) : base(exMessage)
    {
        
    }

}