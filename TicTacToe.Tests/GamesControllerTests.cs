using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using TicTacToe.Application.DTOs;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit;

namespace TicTacToe.Tests.IntegrationTests;

public class GamesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GamesControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        // Создаем фабрику с настроенным логированием
        var factoryWithOutput = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // Очищаем провайдеры по умолчанию
                logging.AddXunit(output); // Добавляем вывод в окно теста
            });
        });

        // Создаем клиент из новой фабрики
        _client = factoryWithOutput.CreateClient();
    }

    [Fact]
    public async Task FullGameFlow_ShouldWorkAsExpected()
    {
        // === Шаг 1: Создаем новую игру ===
        var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var createResponse = await _client.PostAsync("/api/games", emptyContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Получаем ID созданной игры из тела ответа
        var createResult = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var gameId = createResult["gameId"];

        // === Шаг 2: Получаем состояние игры и ETag ===
        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Извлекаем ETag из заголовка ответа
        var etag = getResponse.Headers.ETag;
        etag.Should().NotBeNull();

        // === Шаг 3: Делаем корректный ход, используя ETag ===
        var moveDto = new MakeMoveRequestDto { Player = 'X', Row = 0, Column = 0 };
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = JsonContent.Create(moveDto)
        };
        // Устанавливаем заголовок If-Match, без него будет ошибка 400
        requestMessage.Headers.Add("If-Match", etag.ToString());

        var moveResponse = await _client.SendAsync(requestMessage);
        moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Сохраняем новый ETag, он должен был измениться
        var newEtag = moveResponse.Headers.ETag;
        newEtag.Should().NotBeNull();
        newEtag.Should().NotBe(etag);

        // === Шаг 4: Пытаемся сделать ход со старым ETag и получаем ошибку ===
        var conflictingMoveDto = new MakeMoveRequestDto { Player = 'O', Row = 1, Column = 1 };
        var conflictingRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = JsonContent.Create(conflictingMoveDto)
        };
        // Отправляем старый, невалидный ETag
        conflictingRequest.Headers.Add("If-Match", etag.ToString());

        var conflictResponse = await _client.SendAsync(conflictingRequest);

        // Ожидаем статус 409 Conflict - это доказывает, что наша защита от гонок работает!
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    // В файле GamesControllerTests.cs

// ... (существующий тест FullGameFlow_ShouldWorkAsExpected) ...

    [Fact]
    public async Task MakeMove_WithIdenticalConcurrentRequests_SecondRequestShouldReturnOk()
    {
        // === Arrange: Создаем игру и делаем первый ход ===
        var createResponse =
            await _client.PostAsync("/api/games", new StringContent("{}", Encoding.UTF8, "application/json"));
        var gameId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())["gameId"];

        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        var initialEtag = getResponse.Headers.ETag.ToString();

        var moveDto = new MakeMoveRequestDto { Player = 'X', Row = 0, Column = 0 };
        var moveContent = JsonContent.Create(moveDto);

        var request1 = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = JsonContent.Create(moveDto)
        };
        request1.Headers.Add("If-Match", initialEtag);

        // Создаем ТОЧНО ТАКОЙ ЖЕ второй запрос (симулируем отправку дубликата при проблемах сети)
        var request2 = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = JsonContent.Create(moveDto)
        };
        request2.Headers.Add("If-Match", initialEtag);

        // === Act ===
        // Отправляем первый запрос, он должен пройти успешно
        var response1 = await _client.SendAsync(request1);

        // Отправляем второй, идентичный запрос.
        var response2 = await _client.SendAsync(request2);

        // === Assert ===
        // Первый запрос, как и ожидалось, успешен.
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var etagAfterFirstMove = response1.Headers.ETag;
        etagAfterFirstMove.Should().NotBeNull();

        // Второй запрос, согласно требованию об идемпотентности, тоже должен вернуть 200 OK.
        // Наша логика должна распознать, что это дубликат, а не выдать 409 Conflict.
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var gameDtoFromSecondResponse = await response2.Content.ReadFromJsonAsync<GameDto>();

        // ETag второго ответа должен быть таким же, как у первого (т.е. актуальным).
        response2.Headers.ETag.Should().Be(etagAfterFirstMove);
        // И в теле ответа должно быть состояние игры ПОСЛЕ первого хода.
        gameDtoFromSecondResponse.Board[0][0].Should().Be('X');
        gameDtoFromSecondResponse.CurrentTurn.Should().Be('O');
    }

    [Fact]
    public async Task MakeMove_WithMalformedJsonBody_ShouldReturnBadRequest()
    {
        // Arrange: Создаем игру и получаем ETag
        var createResponse =
            await _client.PostAsync("/api/games", new StringContent("{}", Encoding.UTF8, "application/json"));
        var gameId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())["gameId"];
        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        var etag = getResponse.Headers.ETag.ToString();

        // Content-Type правильный, но сам JSON синтаксически неверный
        var malformedJson = "{\"player\": \"X\", \"row\": 0";
        var requestContent = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = requestContent
        };
        request.Headers.Add("If-Match", etag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Вот здесь мы ожидаем 400, так как парсер JSON не справится
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MakeMove_WithUnsupportedContentType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange: Создаем игру и получаем ETag
        var createResponse =
            await _client.PostAsync("/api/games", new StringContent("{}", Encoding.UTF8, "application/json"));
        var gameId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())["gameId"];
        var getResponse = await _client.GetAsync($"/api/games/{gameId}");
        var etag = getResponse.Headers.ETag.ToString();

        // Тело может быть любым, главное - неправильный Content-Type
        var requestContent = new StringContent("hello world", Encoding.UTF8, "text/plain");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = requestContent
        };
        request.Headers.Add("If-Match", etag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // А здесь фреймворк справедливо вернет 415, так как не умеет работать с text/plain
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task MakeMove_WithoutIfMatchHeader_ShouldReturnBadRequest()
    {
        // Arrange
        var createResponse =
            await _client.PostAsync("/api/games", new StringContent("{}", Encoding.UTF8, "application/json"));
        var gameId = (await createResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())["gameId"];

        var moveDto = new MakeMoveRequestDto { Player = 'X', Row = 0, Column = 0 };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{gameId}/moves")
        {
            Content = JsonContent.Create(moveDto)
        };
        // Намеренно НЕ добавляем заголовок If-Match

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGame_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/games/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}