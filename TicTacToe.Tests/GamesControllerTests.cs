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
                logging.AddXunit(output);   // Добавляем вывод в окно теста
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
}