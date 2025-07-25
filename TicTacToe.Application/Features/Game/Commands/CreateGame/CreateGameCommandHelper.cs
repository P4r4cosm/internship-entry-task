using MediatR;
using Microsoft.Extensions.Configuration;
using TicTacToe.Application.Interfaces;
using TicGame=TicTacToe.Domain.Entities.Game;
namespace TicTacToe.Application.Features.Game.Commands;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
{
    private readonly IGameRepository _gameRepository;
    private readonly IConfiguration _configuration;

    public CreateGameCommandHandler(IGameRepository gameRepository, IConfiguration configuration)
    {
        _gameRepository = gameRepository;
        _configuration = configuration;
    }

    public async Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        // 1. Читаем правила игры из конфигурации
        var boardSizeSection = _configuration.GetSection("BoardSize");
        var winConditionSection = _configuration.GetSection("WinCondition");
    
        var boardSize = int.Parse(boardSizeSection.Value);
        var winCondition = int.Parse(winConditionSection.Value);

        // 2. Создаем игру через фабричный метод
        var game=TicGame.CreateNew(boardSize, winCondition);

        // 3. Добавляем игру в репозиторий
        await _gameRepository.AddAsync(game);

        // 4. Сохраняем изменения
        await _gameRepository.SaveChangesAsync();

        // 5. Возвращаем ID созданной игры
        return game.Id;
    }
}