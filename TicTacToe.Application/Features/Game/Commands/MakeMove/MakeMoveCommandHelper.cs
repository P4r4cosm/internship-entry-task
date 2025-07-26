using AutoMapper;
using MediatR;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Interfaces;
using TicTacToe.Domain.Common;

namespace TicTacToe.Application.Features.Game.Commands.MakeMove;

public class MakeMoveCommandHandler : IRequestHandler<MakeMoveCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    private static readonly Random _random = new Random();

    public MakeMoveCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<GameDto> Handle(MakeMoveCommand request, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Game), request.GameId);
        }

        // ===== ПРОВЕРКА НА ИДЕМПОТЕНТНОСТЬ =====
        // Находим самый последний ход в игре
        var lastMove = game.Moves.OrderByDescending(m => m.MoveNumber).FirstOrDefault();

        // Если последний ход существует и он полностью идентичен текущему запросу
        if (lastMove != null &&
            lastMove.Row == request.Row &&
            lastMove.Column == request.Column &&
            game.CurrentTurn == Player.GetOpponent(request.Player)) // Проверяем, что ход был сделан игроком из запроса
        {
            // Это повторный запрос. Просто возвращаем текущее состояние игры.
            // Клиент получит 200 OK и актуальный ETag.
            return _mapper.Map<GameDto>(game);
        }
        // =======================================================


        // Проверка ETag (защита от состояния гонки для РАЗНЫХ ходов)
        if (game.Version != request.ETag)
        {
            throw new ConflictException("The game state has changed. Please refresh and try again.");
        }

        try
        {
            // Вызываем доменную логику
            game.MakeMove(request.Player, request.Row, request.Column, maxValue => _random.Next(maxValue));
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new BadRequestException(ex.Message);
        }


        await _gameRepository.SaveChangesAsync(cancellationToken);


        return _mapper.Map<GameDto>(game);
    }
}