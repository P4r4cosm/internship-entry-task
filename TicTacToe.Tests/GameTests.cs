using System;
using FluentAssertions;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;
using TicTacToe.Domain.Common;
using Xunit;

public class GameDomainTests
{
    // Вспомогательная функция для генератора случайных чисел.
    // По умолчанию она возвращает число > 10, чтобы специальное правило не срабатывало.
    private readonly Func<int, int> _predictableRandomProvider = _ => 100;

    #region Тесты на создание игры

    [Fact]
    public void CreateNew_WithValidParameters_ShouldCreateGameCorrectly()
    {
        // Arrange
        int boardSize = 3;
        int winCondition = 3;

        // Act
        var game = Game.CreateNew(boardSize, winCondition);

        // Assert
        game.Id.Should().NotBe(Guid.Empty);
        game.Version.Should().Be(Guid.Empty); // Начальная версия может быть пустой до первого хода
        game.BoardSize.Should().Be(boardSize);
        game.WinCondition.Should().Be(winCondition);
        game.Status.Should().Be(GameStatus.InProgress);
        game.CurrentTurn.Should().Be(Player.X);
        game.Moves.Should().BeEmpty();
    }

    [Theory]
    [InlineData(2, 3)]  // Размер доски слишком мал
    [InlineData(3, 4)]  // Условие победы больше размера доски
    public void CreateNew_WithInvalidParameters_ShouldThrowArgumentException(int boardSize, int winCondition)
    {
        // Arrange
        Action createAction = () => Game.CreateNew(boardSize, winCondition);

        // Act & Assert
        createAction.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Тесты на логику ходов

    [Fact]
    public void MakeMove_OnValidTurn_ShouldUpdateStateAndChangeVersion()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        var initialVersion = game.Version;

        // Act
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider);

        // Assert
        game.Moves.Count.Should().Be(1);
        var lastMove = game.Moves.First();
        lastMove.Player.Should().Be(Player.X);
        lastMove.Row.Should().Be(0);
        lastMove.Column.Should().Be(0);
        
        game.CurrentTurn.Should().Be(Player.O);
        game.Version.Should().NotBe(initialVersion);
        game.Status.Should().Be(GameStatus.InProgress);
    }

    [Fact]
    public void MakeMove_WhenNotPlayersTurn_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);

        // Act
        Action act = () => game.MakeMove(Player.O, 0, 0, _predictableRandomProvider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"It's not player {Player.O}'s turn.");
    }

    [Fact]
    public void MakeMove_ToOccupiedCell_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider);

        // Act
        Action act = () => game.MakeMove(Player.O, 0, 0, _predictableRandomProvider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("This cell is already occupied.");
    }
    
    [Fact]
    public void MakeMove_OutOfBounds_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);

        // Act
        Action act = () => game.MakeMove(Player.X, 3, 3, _predictableRandomProvider);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
    
    #region Тесты на специальное правило (10% шанс)

    [Fact]
    public void MakeMove_OnThirdTurnWithUnluckyRoll_ShouldPlaceOpponentSymbol()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider); // Ход 1
        game.MakeMove(Player.O, 1, 0, _predictableRandomProvider); // Ход 2

        // На 3-м ходу "не везет", генератор возвращает 5 (что < 10)
        var unluckyRandomProvider = new Func<int, int>(_ => 5);

        // Act: Игрок X пытается сделать третий ход
        game.MakeMove(Player.X, 2, 0, unluckyRandomProvider);

        // Assert
        var lastMove = game.Moves.Last();
        lastMove.Player.Should().Be(Player.O); // Проверяем, что фигура поставилась от имени противника
        lastMove.Row.Should().Be(2);
        lastMove.Column.Should().Be(0);
        
        game.Status.Should().Be(GameStatus.InProgress);
        // Очередь хода НЕ переключается, так как ход был "испорчен"
        game.CurrentTurn.Should().Be(Player.O); 
    }
    
    [Fact]
    public void MakeMove_OnThirdTurnWithLuckyRoll_ShouldPlaceOwnSymbol()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider); // Ход 1
        game.MakeMove(Player.O, 1, 0, _predictableRandomProvider); // Ход 2
        
        // На 3-м ходу "везет", генератор возвращает > 10
        var luckyRandomProvider = new Func<int, int>(_ => 50);

        // Act: Игрок X делает третий ход
        game.MakeMove(Player.X, 2, 0, luckyRandomProvider);

        // Assert
        var lastMove = game.Moves.Last();
        lastMove.Player.Should().Be(Player.X); // Фигура поставилась от своего имени
        game.CurrentTurn.Should().Be(Player.O); // Очередь хода переключилась
    }

    #endregion

    #region Тесты на завершение игры (Победа/Ничья)

    [Fact]
    public void MakeMove_ThatCompletesHorizontalWin_ShouldUpdateStatusToXWins()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);

        // Act
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider); // X
        game.MakeMove(Player.O, 1, 0, _predictableRandomProvider); // O
        game.MakeMove(Player.X, 0, 1, _predictableRandomProvider); // X
        game.MakeMove(Player.O, 1, 1, _predictableRandomProvider); // O
        game.MakeMove(Player.X, 0, 2, _predictableRandomProvider); // X делает выигрышный ход

        // Assert
        game.Status.Should().Be(GameStatus.XWins);
    }
    
    [Fact]
    public void MakeMove_ThatFillsBoardWithNoWinner_ShouldUpdateStatusToDraw()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);

        // Act
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider);
        game.MakeMove(Player.O, 0, 1, _predictableRandomProvider);
        game.MakeMove(Player.X, 0, 2, _predictableRandomProvider);
        game.MakeMove(Player.O, 1, 2, _predictableRandomProvider);
        game.MakeMove(Player.X, 1, 0, _predictableRandomProvider);
        game.MakeMove(Player.O, 1, 1, _predictableRandomProvider);
        game.MakeMove(Player.X, 2, 1, _predictableRandomProvider);
        game.MakeMove(Player.O, 2, 0, _predictableRandomProvider);
        game.MakeMove(Player.X, 2, 2, _predictableRandomProvider); // Последний ход, приводящий к ничьей

        // Assert
        game.Status.Should().Be(GameStatus.Draw);
        game.Moves.Count.Should().Be(9);
    }

    [Fact]
    public void MakeMove_WhenGameIsOver_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        // Доводим игру до победы
        game.MakeMove(Player.X, 0, 0, _predictableRandomProvider);
        game.MakeMove(Player.O, 1, 0, _predictableRandomProvider);
        game.MakeMove(Player.X, 0, 1, _predictableRandomProvider);
        game.MakeMove(Player.O, 1, 1, _predictableRandomProvider);
        game.MakeMove(Player.X, 0, 2, _predictableRandomProvider);

        game.Status.Should().Be(GameStatus.XWins); // Убедились, что игра окончена

        // Act
        Action act = () => game.MakeMove(Player.O, 2, 2, _predictableRandomProvider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Game is already over.");
    }

    #endregion
}