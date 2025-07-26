using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Common.Exceptions;
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        // Ловим специфичное для EF Core исключение здесь, в Infrastructure
        catch (DbUpdateConcurrencyException ex)
        {
            // И бросаем вместо него общее исключение, понятное Application слою
            throw new ConflictException("The game state has changed. Please refresh and try again.");
        }
    }
}