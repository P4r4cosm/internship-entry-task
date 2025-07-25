using TicTacToe.Domain.Common;
using TicTacToe.Domain.Enums;


namespace TicTacToe.Domain.Entities;
public class Game
{
    // Константы для правил игры, чтобы избежать "магических чисел"
    private const int SpecialMoveInterval = 3;
    private const int OpponentMoveChancePercentage = 10;
    private const int MaxPercentage = 100;

    public Guid Id { get; private set; }
    public int BoardSize { get; private set; }
    public int WinCondition { get; private set; }
    public GameStatus Status { get; private set; }
    public char CurrentTurn { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    
    private char?[,] _board;
    private readonly List<Move> _moves = new();
    public IReadOnlyCollection<Move> Moves => _moves.AsReadOnly();

    // Приватный конструктор для использования с фабричным методом и ORM
    private Game() { }

    /// <summary>
    /// Фабричный метод для создания новой игры.
    /// </summary>
    public static Game CreateNew(int boardSize, int winCondition)
    {
        if (boardSize < 3) throw new ArgumentException("Board size must be at least 3.", nameof(boardSize));
        if (winCondition > boardSize)
            throw new ArgumentException("Win condition cannot be greater than board size.", nameof(winCondition));

        var game = new Game
        {
            Id = Guid.NewGuid(),
            BoardSize = boardSize,
            WinCondition = winCondition,
            Status = GameStatus.InProgress,
            CurrentTurn = Player.X,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Инициализируем пустое поле один раз при создании игры
        game._board = new char?[boardSize, boardSize];

        return game;
    }

    /// <summary>
    /// Выполняет ход игрока и обновляет состояние игры.
    /// </summary>
    /// <param name="player">Игрок, делающий ход ('X' или 'O').</param>
    /// <param name="row">Строка для хода.</param>
    /// <param name="column">Колонка для хода.</param>
    /// <param name="randomProvider">Функция для генерации случайного числа (для тестируемости).</param>
    public void MakeMove(char player, int row, int column, Func<int, int> randomProvider)
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is already over.");

        if (player != CurrentTurn)
            throw new InvalidOperationException($"It's not player {player}'s turn.");

        if (!IsInBounds(row, column))
            throw new ArgumentOutOfRangeException("Move is outside the board.");

        if (_board[row, column].HasValue)
            throw new InvalidOperationException("This cell is already occupied.");

        var moveNumber = _moves.Count + 1;
        var finalPlayer = player;

        // Реализация правила: каждый 3-й ход есть 10% шанс, что поставится фигура противника
        if (moveNumber % SpecialMoveInterval == 0)
        {
            var chance = randomProvider(MaxPercentage);
            if (chance < OpponentMoveChancePercentage)
            {
                finalPlayer = Player.GetOpponent(player);
            }
        }

        var move = new Move(this.Id, finalPlayer, row, column, moveNumber);
        _moves.Add(move);
        _board[move.Row, move.Column] = move.Player;

        UpdateGameStatusAfterMove(move);

        if (Status == GameStatus.InProgress)
        {
            CurrentTurn = Player.GetOpponent(player);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверяет, завершилась ли игра после последнего хода.
    /// </summary>
    private void UpdateGameStatusAfterMove(Move lastMove)
    {
        if (CheckForWin(lastMove))
        {
            Status = lastMove.Player == Player.X ? GameStatus.XWins : GameStatus.OWins;
        }
        else if (_moves.Count == BoardSize * BoardSize)
        {
            Status = GameStatus.Draw;
        }
    }

    /// <summary>
    /// Проверяет наличие выигрышной комбинации, проходящей через последний ход.
    /// </summary>
    private bool CheckForWin(Move lastMove)
    {
        char player = lastMove.Player;
        int row = lastMove.Row;
        int col = lastMove.Column;

        // Проверка по всем 4-м направлениям (горизонталь, вертикаль, 2 диагонали)
        if (CheckDirection(row, col, 1, 0, player) >= WinCondition ||
            CheckDirection(row, col, 0, 1, player) >= WinCondition ||
            CheckDirection(row, col, 1, 1, player) >= WinCondition ||
            CheckDirection(row, col, 1, -1, player) >= WinCondition)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Считает количество непрерывных символов игрока в заданном направлении.
    /// </summary>
    private int CheckDirection(int row, int col, int dRow, int dCol, char player)
    {
        // Считаем текущую клетку
        int count = 1;

        // Движемся в одном направлении
        for (int i = 1; i < WinCondition; i++)
        {
            int nextRow = row + i * dRow;
            int nextCol = col + i * dCol;
            if (IsInBounds(nextRow, nextCol) && _board[nextRow, nextCol] == player)
                count++;
            else
                break;
        }

        // Движемся в обратном направлении
        for (int i = 1; i < WinCondition; i++)
        {
            int nextRow = row - i * dRow;
            int nextCol = col - i * dCol;
            if (IsInBounds(nextRow, nextCol) && _board[nextRow, nextCol] == player)
                count++;
            else
                break;
        }

        return count;
    }

    /// <summary>
    /// Вспомогательный метод для проверки, находятся ли координаты в пределах поля.
    /// </summary>
    private bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
    }
    
    /// <summary>
    /// Фабричный метод для восстановления существующей игры из истории (например, из базы данных).
    /// </summary>
    /// <returns>Полностью сконфигурированный и готовый к работе объект Game.</returns>
    public static Game LoadFromHistory(
        Guid id,
        int boardSize,
        int winCondition,
        GameStatus status,
        char currentTurn,
        DateTime createdAt,
        DateTime updatedAt,
        ICollection<Move> moves)
    {
        var game = new Game
        {
            Id = id,
            BoardSize = boardSize,
            WinCondition = winCondition,
            Status = status,
            CurrentTurn = currentTurn,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // --- Процесс "Гидратации" ---
        // 1. Инициализируем доску
        game._board = new char?[boardSize, boardSize];

        // 2. Заполняем приватный список ходов и одновременно "отрисовываем" доску
        foreach (var move in moves)
        {
            game._moves.Add(move);
            game._board[move.Row, move.Column] = move.Player;
        }

        return game;
    }
}
