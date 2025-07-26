// В тестовом проекте, например, в TicTacToe.Application.Tests/MakeMoveCommandHandlerTests.cs

using Moq;
using AutoMapper;
using FluentAssertions;
using TicTacToe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Features.Game.Commands.MakeMove;
using TicTacToe.Application.Interfaces;
using Xunit;

public class MakeMoveCommandHandlerTests
{
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly MakeMoveCommandHandler _handler;

    public MakeMoveCommandHandlerTests()
    {
        _mockGameRepository = new Mock<IGameRepository>();
        _mockMapper = new Mock<IMapper>();
        // Создаем экземпляр нашего обработчика с моками вместо реальных зависимостей
        _handler = new MakeMoveCommandHandler(_mockGameRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
    {
        // Arrange
        var command = new MakeMoveCommand { GameId = Guid.NewGuid() };
        // Настраиваем мок репозитория, чтобы он возвращал null
        _mockGameRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenETagIsIncorrect()
    {
        // Arrange
        // Создаем игру с какой-то версией
        var game = Game.CreateNew(3, 3);
        // В команде передаем другую, неправильную версию
        var command = new MakeMoveCommand { GameId = game.Id, ETag = Guid.NewGuid() };

        _mockGameRepository.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Проверяем, что была выброшена именно ошибка конфликта
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_OnDbUpdateConcurrencyException()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        // ETag совпадает, так что первая проверка пройдет
        var command = new MakeMoveCommand { GameId = game.Id, ETag = game.Version, Player = 'X', Row = 0, Column = 0 };

        _mockGameRepository.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Настраиваем мок так, чтобы при вызове SaveChangesAsync он имитировал ошибку гонки от EF Core
        _mockGameRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallMakeMoveAndSaveChanges()
    {
        // Arrange
        var game = Game.CreateNew(3, 3);
        var command = new MakeMoveCommand { GameId = game.Id, ETag = game.Version, Player = 'X', Row = 0, Column = 0 };

        _mockGameRepository.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mockGameRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Имитируем успешное сохранение

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Убедимся, что репозиторий был вызван для сохранения изменений ровно один раз.
        _mockGameRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        // Убедимся, что результат был смаплен.
        _mockMapper.Verify(m => m.Map<GameDto>(It.IsAny<Game>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithDuplicateMoveRequest_ShouldReturnSuccessWithoutModifyingState()
    {
        // Arrange
        // Создаем игру и делаем в ней первый ход.
        var game = Game.CreateNew(3, 3);
        game.MakeMove('X', 0, 0, _ => 100); // Сделали ход X на (0,0)

        // Теперь CurrentTurn = 'O', а Version изменилась.

        // Создаем команду, которая в точности дублирует уже сделанный ход.
        var duplicateCommand = new MakeMoveCommand { GameId = game.Id, Player = 'X', Row = 0, Column = 0 };

        _mockGameRepository.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        await _handler.Handle(duplicateCommand, CancellationToken.None);

        // Assert
        // Самое главное: SaveChangesAsync НЕ должен быть вызван, так как это дубликат.
        _mockGameRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());

        // Мы все равно должны вернуть клиенту актуальное состояние игры.
        _mockMapper.Verify(m => m.Map<GameDto>(It.IsAny<Game>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WhenDomainThrowsInvalidOperation_ShouldThrowBadRequestException()
    {
        // Arrange
        // Создаем игру и делаем ход.
        var game = Game.CreateNew(3, 3);
        game.MakeMove('X', 0, 0, _ => 100);

        // Создаем команду для хода в ту же клетку, но уже от другого игрока.
        // Это не дубликат (другой игрок), поэтому проверка на идемпотентность не сработает.
        var command = new MakeMoveCommand { GameId = game.Id, ETag = game.Version, Player = 'O', Row = 0, Column = 0 };

        _mockGameRepository.Setup(r => r.GetByIdAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Доменная логика `game.MakeMove` выбросит InvalidOperationException("This cell is already occupied.").
        // Наш обработчик должен поймать его и преобразовать в BadRequestException.
        await act.Should().ThrowAsync<BadRequestException>();
    }
}