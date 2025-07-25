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
            .ForMember(dest => dest.Board, opt => opt.MapFrom(src => MapBoard(src)));
    }

    // Логика маппинга теперь возвращает char?[][]
    private char?[][] MapBoard(Game game)
    {
        // Создаем рваный массив
        var board = new char?[game.BoardSize][];
        for (int i = 0; i < game.BoardSize; i++)
        {
            board[i] = new char?[game.BoardSize];
        }

        // Заполняем его
        foreach (var move in game.Moves)
        {
            board[move.Row][move.Column] = move.Player;
        }
        return board;
    }
}