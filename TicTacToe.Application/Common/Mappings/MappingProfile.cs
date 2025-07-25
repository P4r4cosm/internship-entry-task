using AutoMapper;
using TicTacToe.Application.DTOs;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Move, MoveDto>();
        CreateMap<Game, GameDto>()
            // Настраиваем кастомный маппинг для поля Board
            .ForMember(dest => dest.Board, opt => opt.MapFrom(src => MapBoard(src)));
    }

    private char?[,] MapBoard(Game game)
    {
        var board = new char?[game.BoardSize, game.BoardSize];
        foreach (var move in game.Moves)
        {
            board[move.Row, move.Column] = move.Player;
        }
        return board;
    }
}