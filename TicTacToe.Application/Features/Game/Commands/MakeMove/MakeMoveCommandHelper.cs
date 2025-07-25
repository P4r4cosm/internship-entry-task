using AutoMapper;
using MediatR;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Interfaces;

namespace TicTacToe.Application.Features.Game.Commands.MakeMove;

public class MakeMoveCommandHandler : IRequestHandler<MakeMoveCommand, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    
    // Создаем один экземпляр Random для этого обработчика
    private static readonly Random _random = new Random();

    public MakeMoveCommandHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<GameDto> Handle(MakeMoveCommand request, CancellationToken cancellationToken)
    {
        // 1. Загружаем игру. Это критически важный шаг.
        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Game), request.GameId);
        }

        // 2. Вызываем доменный метод, передавая ему настоящую реализацию генератора случайных чисел.
        //    Вся сложная логика проверки (чья очередь, занята ли клетка) инкапсулирована в домене.
        //    Если что-то пойдет не так, домен бросит исключение, которое мы можем перехватить.
        try
        {
            game.MakeMove(request.Player, request.Row, request.Column, maxValue => _random.Next(maxValue));
        }
        catch (InvalidOperationException ex)
        {
            // Превращаем доменное исключение в более специфичное для API
            throw new BadRequestException(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new BadRequestException(ex.Message);
        }

        // 3. Сохраняем измененное состояние игры
        //    Важно: GetByIdAsync и SaveChangesAsync должны работать в рамках одной транзакции,
        //    что обычно обеспечивается временем жизни DbContext'а в ASP.NET Core (scoped).
        await _gameRepository.SaveChangesAsync(cancellationToken);
        
        // 4. Возвращаем новое состояние игры клиенту
        return _mapper.Map<GameDto>(game);
    }
}

