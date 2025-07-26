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
        // Загружаем отслеживаемую EF Core сущность, включая связанные ходы.
        var game = await _context.Games
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (game == null)
        {
            return null;
        }
    
        // "Гидрируем" приватное состояние доски.
        game.HydrateBoard();

        // Возвращаем отслеживаемую сущность.
        return game;
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