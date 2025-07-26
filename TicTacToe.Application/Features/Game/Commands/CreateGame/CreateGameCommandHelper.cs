using MediatR;
using TicTacToe.Application.Interfaces;
using TicGame=TicTacToe.Domain.Entities.Game;
namespace TicTacToe.Application.Features.Game.Commands;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
{
    private readonly IGameRepository _gameRepository;

    public CreateGameCommandHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        
        // 1. Создаем игру через фабричный метод
        var game=TicGame.CreateNew(request.BoardSize, request.WinCondition);

        // 2. Добавляем игру в репозиторий
        await _gameRepository.AddAsync(game);

        // 3. Сохраняем изменения
        await _gameRepository.SaveChangesAsync();

        // 4. Возвращаем ID созданной игры
        return game.Id;
    }
}