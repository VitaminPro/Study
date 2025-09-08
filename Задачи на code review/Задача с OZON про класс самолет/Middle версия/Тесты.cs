
/*
На собеседовании после рефакторинга обычно спрашивают: «А как ты это протестируешь?»
Поэтому сделаем юнит-тест на FlightService.StartFlightAsync с моками Kafka и DbContext.
Unit-тест FlightService
Будем использовать:
xUnit как тестовый фреймворк,
Moq для моков зависимостей,
InMemoryDbContext (EF Core) для удобства.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class FlightServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public FlightServiceTests()
    {
        // Используем InMemory базу, чтобы не тянуть реальную SQL
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task StartFlightAsync_SavesFlightAndPublishesEvent()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);

        var kafkaMock = new Mock<IKafkaProducer>();
        var loggerMock = new Mock<ILogger<FlightService>>();

        var service = new FlightService(dbContext, kafkaMock.Object, loggerMock.Object);

        var pilot = new Person { FirstName = "John", LastName = "Doe", DocumentId = "DOC123" };
        var plane = new Plane(42, pilot);
        plane.RegisterPassenger(new Person { FirstName = "Alice", LastName = "Smith", DocumentId = "DOC999" });

        var token = CancellationToken.None;

        // Act
        await service.StartFlightAsync(plane, token);

        // Assert
        // 1. Проверяем, что рейс сохранился в базе
        var flight = await dbContext.Flights.FirstOrDefaultAsync(f => f.FlightId == 42);
        Assert.NotNull(flight);
        Assert.Contains("Alice", flight!.PassengersJson);

        // 2. Проверяем, что событие Outbox записано
        var outbox = await dbContext.Outbox.FirstOrDefaultAsync(e => e.EventType == "FlightStarted");
        Assert.NotNull(outbox);
        Assert.Contains("42", outbox!.Payload);

        // 3. Проверяем, что Kafka вызван один раз
        kafkaMock.Verify(k => k.SendAsync("flights", It.IsAny<string>(), token), Times.Once);

        // 4. Проверяем, что логгер записал успешное сообщение
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
