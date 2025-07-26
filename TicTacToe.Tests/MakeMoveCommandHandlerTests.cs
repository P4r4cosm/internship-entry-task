// В тестовом проекте, например, в TicTacToe.Application.Tests/MakeMoveCommandHandlerTests.cs

using Moq;
using AutoMapper;
using FluentAssertions;
using TicTacToe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Common.Exceptions;
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
}