using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Interfaces;

public interface IGameRepository
{
    /// <summary>
    /// Находит игру по ее ID и возвращает полностью "гидрированный" объект.
    /// </summary>
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет новую игру в контекст для отслеживания.
    /// </summary>
    Task AddAsync(Game game, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет все изменения, сделанные в контексте, в базу данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}