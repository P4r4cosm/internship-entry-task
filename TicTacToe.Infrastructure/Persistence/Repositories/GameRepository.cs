using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Interfaces;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Persistence.Repositories;

public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;

    public GameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Загружаем "сырые" данные из БД, обязательно включая связанные ходы.
        // AsNoTracking() полезен, т.к. мы создадим новый объект через фабрику,
        // а не будем изменять тот, что отслеживает EF.
        var gameData = await _context.Games
            .AsNoTracking()
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (gameData == null)
        {
            return null;
        }
        
        return Game.LoadFromHistory(
            gameData.Id,
            gameData.BoardSize,
            gameData.WinCondition,
            gameData.Status,
            gameData.CurrentTurn,
            gameData.CreatedAt,
            gameData.UpdatedAt,
            gameData.Moves
        );
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _context.Games.AddAsync(game, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}