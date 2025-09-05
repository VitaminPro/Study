// Импорты - только то что нужно, без избыточности
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// ============================================================================
// VALUE OBJECTS - Неизменяемые объекты-значения по принципам DDD
// ============================================================================

/// <summary>
/// Strongly-typed ID для рейса. Предотвращает путаницу между разными типами ID
/// readonly struct - value type, неизменяемый, минимальное выделение памяти
/// </summary>
public readonly record struct FlightId(int Value)
{
    // implicit операторы для удобства использования без явного приведения типов
    public static implicit operator int(FlightId flightId) => flightId.Value;
    public static implicit operator FlightId(int value) => new(value);
}

/// <summary>
/// Strongly-typed ID для пассажира. Исключает ошибки смешивания ID
/// </summary>
public readonly record struct PassengerId(int Value)
{
    public static implicit operator int(PassengerId id) => id.Value;
    public static implicit operator PassengerId(int value) => new(value);
}

/// <summary>
/// Value Object для координат. Инкапсулирует валидацию и бизнес-логику
/// </summary>
public readonly record struct Coordinate(double Latitude, double Longitude)
{
    // Бизнес-правило: валидные координаты Земли
    // Computed property - вычисляется каждый раз, но struct легковесен
    public bool IsValid => Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180;
}

/// <summary>
/// Value Object для ID документа с встроенной валидацией
/// </summary>
public readonly record struct DocumentId(string Value)
{
    // Конструктор с валидацией - fail-fast принцип
    public DocumentId(string value) : this() // вызов базового конструктора struct
    {
        // Guard clause - ранний выход при невалидных данных
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Document ID cannot be empty", nameof(value));
        
        // Normalization - приведение к стандартному виду
        Value = value.Trim();
    }

    // Implicit conversions для удобства работы
    public static implicit operator string(DocumentId documentId) => documentId.Value;
    public static implicit operator DocumentId(string value) => new(value);
}

// ============================================================================
// DOMAIN ENTITIES - Сущности предметной области
// ============================================================================

/// <summary>
/// Интерфейс для Person - позволяет мокать в тестах и следует DIP
/// Только read-only свойства - immutability по умолчанию
/// </summary>
public interface IPerson
{
    DocumentId DocumentId { get; } // get-only property - неизменяемость
    string FirstName { get; }
    string LastName { get; }
    string FullName { get; } // computed property
}

/// <summary>
/// Immutable Person entity - после создания не может быть изменен
/// sealed - предотвращает наследование, оптимизация компилятора
/// </summary>
public sealed class Person : IPerson
{
    // Конструктор с полной валидацией - defensive programming
    public Person(DocumentId documentId, string firstName, string lastName)
    {
        DocumentId = documentId; // value object уже валидирован
        
        // Null-safety с информативными сообщениями
        FirstName = firstName?.Trim() ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName?.Trim() ?? throw new ArgumentNullException(nameof(lastName));

        // Бизнес-валидация после нормализации
        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        if (string.IsNullOrWhiteSpace(LastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
    }

    // Init-only properties - устанавливаются только в конструкторе
    public DocumentId DocumentId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    
    // Computed property - не занимает память, вычисляется при обращении
    public string FullName => $"{FirstName} {LastName}";

    // Override ToString для удобства отладки и логирования
    public override string ToString() => FullName;
}

// ============================================================================
// DOMAIN EVENTS - События предметной области для Event-Driven Architecture
// ============================================================================

/// <summary>
/// Базовый класс для всех доменных событий
/// abstract - нельзя создать экземпляр, только наследовать
/// record - автоматическая реализация Equals, GetHashCode, ToString
/// </summary>
public abstract record DomainEvent(DateTime OccurredAt);

/// <summary>
/// Событие регистрации пассажира
/// sealed record - финальный тип, оптимизация, неизменяемость
/// </summary>
public sealed record PassengerRegisteredEvent(
    DateTime OccurredAt,           // Время события
    FlightId FlightId,             // На каком рейсе
    PassengerId PassengerId,       // ID созданного пассажира
    IPerson Passenger              // Сам пассажир
) : DomainEvent(OccurredAt);

/// <summary>
/// Событие смены пилота
/// </summary>
public sealed record PilotChangedEvent(
    DateTime OccurredAt,
    FlightId FlightId,
    IPerson PreviousPilot,         // Предыдущий пилот для аудита
    IPerson NewPilot               // Новый пилот
) : DomainEvent(OccurredAt);

/// <summary>
/// Событие обновления координат
/// </summary>
public sealed record CoordinateUpdatedEvent(
    DateTime OccurredAt,
    FlightId FlightId,
    Coordinate? PreviousCoordinate, // Nullable - может быть первое обновление
    Coordinate NewCoordinate
) : DomainEvent(OccurredAt);

/// <summary>
/// Событие запуска рейса
/// </summary>
public sealed record FlightStartedEvent(
    DateTime OccurredAt,
    FlightId FlightId,
    IPerson Pilot,
    IReadOnlyCollection<IPerson> Passengers, // ReadOnly - предотвращает изменения
    Coordinate? StartingCoordinate           // Nullable - координаты могут быть неизвестны
) : DomainEvent(OccurredAt);

// ============================================================================
// SERVICE INTERFACES - Контракты для внешних зависимостей
// ============================================================================

/// <summary>
/// Интерфейс навигационного сервиса - следует Interface Segregation Principle
/// </summary>
public interface INavigationService
{
    // Async method с CancellationToken по умолчанию - best practice
    Task<Coordinate> GetCurrentCoordinateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Репозиторий для сохранения данных рейса - DIP принцип
/// </summary>
public interface IFlightRepository
{
    // ReadOnlyCollection - гарантирует, что репозиторий не изменит данные
    Task SaveFlightAsync(FlightId flightId, IPerson pilot, 
        IReadOnlyCollection<IPerson> passengers, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publisher для доменных событий - Event-Driven Architecture
/// </summary>
public interface IFlightEventPublisher
{
    // Generic constraint - работает только с доменными событиями
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) 
        where T : DomainEvent;
}

// ============================================================================
// RESULT PATTERN - Функциональный подход к обработке ошибок
// ============================================================================

/// <summary>
/// Базовый класс Result pattern - альтернатива исключениям
/// abstract - нельзя создать напрямую, только через наследников
/// </summary>
public abstract class Result<T>
{
    // abstract properties - обязательны к реализации в наследниках
    public abstract bool IsSuccess { get; }
    public abstract bool IsFailure { get; }
    public abstract T Value { get; }
    public abstract string Error { get; }

    // Static factory methods - удобное создание результатов
    public static Result<T> Success(T value) => new SuccessResult<T>(value);
    public static Result<T> Failure(string error) => new FailureResult<T>(error);
}

/// <summary>
/// Успешный результат операции
/// sealed - финальная реализация, оптимизация виртуальных вызовов
/// </summary>
public sealed class SuccessResult<T> : Result<T>
{
    // Конструктор принимает значение
    public SuccessResult(T value) => Value = value;
    
    // Override abstract properties - конкретная реализация
    public override bool IsSuccess => true;
    public override bool IsFailure => false;
    public override T Value { get; } // init-only через конструктор
    public override string Error => string.Empty; // успех = нет ошибки
}

/// <summary>
/// Неуспешный результат операции
/// </summary>
public sealed class FailureResult<T> : Result<T>
{
    public FailureResult(string error) => Error = error;
    
    public override bool IsSuccess => false;
    public override bool IsFailure => true;
    
    // При неудаче нельзя получить Value - исключение
    public override T Value => throw new InvalidOperationException("Cannot access value of failed result");
    public override string Error { get; }
}

// ============================================================================
// MAIN DOMAIN ENTITY - Основная бизнес-сущность
// ============================================================================

/// <summary>
/// Aggregate Root - центральная сущность, управляющая своими компонентами
/// sealed - предотвращает наследование, четко определенная ответственность
/// </summary>
public sealed class Plane
{
    // ========================================================================
    // FIELDS - Внутреннее состояние класса
    // ========================================================================
    
    // readonly - устанавливается только в конструкторе, immutable после создания
    private readonly FlightId _flightId;                    // ID рейса
    private readonly INavigationService _navigationService;  // Зависимость для навигации
    private readonly IFlightRepository _flightRepository;   // Зависимость для сохранения
    private readonly IFlightEventPublisher _eventPublisher; // Зависимость для событий
    private readonly ILogger<Plane> _logger;               // Зависимость для логирования
    
    // Thread-safe коллекция для пассажиров - может изменяться параллельно
    private readonly ConcurrentDictionary<PassengerId, IPerson> _passengers = new();
    
    // ReaderWriterLockSlim - оптимизация для частого чтения, редкой записи координат
    private readonly ReaderWriterLockSlim _coordinateLock = new();
    
    // object для lock - простая синхронизация пилота
    private readonly object _pilotLock = new object();
    
    // Счетчик для генерации ID пассажиров - int потокобезопасен с Interlocked
    private int _nextPassengerId;
    
    // volatile - гарантирует видимость изменений между потоками без блокировки
    private volatile IPerson _pilot;
    
    // Nullable - координаты могут быть неизвестны на момент создания
    private Coordinate? _currentCoordinate;

    // ========================================================================
    // CONSTRUCTOR - Dependency Injection через конструктор
    // ========================================================================
    
    /// <summary>
    /// Конструктор с полной валидацией всех зависимостей
    /// </summary>
    public Plane(
        FlightId flightId,                          // Value object с валидацией
        IPerson pilot,                              // Интерфейс для тестируемости
        INavigationService navigationService,       // DI - навигация
        IFlightRepository flightRepository,         // DI - персистентность
        IFlightEventPublisher eventPublisher,       // DI - события
        ILogger<Plane> logger)                      // DI - логирование
    {
        // Guard clauses - проверки в начале, fail-fast
        if (flightId.Value <= 0)
            throw new ArgumentException("Flight ID must be positive", nameof(flightId));

        // ?? throw pattern - краткая проверка на null
        _flightId = flightId;
        _pilot = pilot ?? throw new ArgumentNullException(nameof(pilot));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _flightRepository = flightRepository ?? throw new ArgumentNullException(nameof(flightRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========================================================================
    // PUBLIC PROPERTIES - Read-only интерфейс для внешнего мира
    // ========================================================================
    
    // get-only свойства - инкапсуляция, нельзя изменить извне
    public FlightId Id => _flightId;
    public IPerson Pilot => _pilot; // volatile обеспечивает актуальность
    
    // Snapshot коллекции - ToArray() создает копию на момент вызова
    public IReadOnlyCollection<IPerson> Passengers => _passengers.Values.ToArray();
    
    // Быстрый доступ к количеству без копирования коллекции
    public int PassengerCount => _passengers.Count;

    // Потокобезопасное чтение координат через ReaderWriterLockSlim
    public Coordinate? CurrentCoordinate
    {
        get
        {
            _coordinateLock.EnterReadLock(); // Блокировка на чтение
            try
            {
                return _currentCoordinate;   // Быстрое чтение
            }
            finally
            {
                _coordinateLock.ExitReadLock(); // Обязательное освобождение
            }
        }
    }

    // ========================================================================
    // BUSINESS METHODS - Основные бизнес-операции
    // ========================================================================

    /// <summary>
    /// Регистрация пассажира с полной обработкой ошибок
    /// </summary>
    /// <param name="passenger">Пассажир для регистрации</param>
    /// <returns>Result с ID пассажира или ошибкой</returns>
    public async Task<Result<PassengerId>> RegisterPassengerAsync(IPerson passenger)
    {
        // Modern C# null check - краткая проверка
        ArgumentNullException.ThrowIfNull(passenger);

        try
        {
            // Атомарная операция инкремента - потокобезопасно
            var passengerId = new PassengerId(Interlocked.Increment(ref _nextPassengerId));
            
            // TryAdd - потокобезопасное добавление в словарь
            if (!_passengers.TryAdd(passengerId, passenger))
            {
                return Result<PassengerId>.Failure($"Failed to register passenger {passenger.FullName}");
            }

            // Создание доменного события для уведомления других компонентов
            var domainEvent = new PassengerRegisteredEvent(
                DateTime.UtcNow, _flightId, passengerId, passenger);

            // Асинхронная публикация события - не блокирует основной поток
            await _eventPublisher.PublishAsync(domainEvent);

            // Structured logging - параметризованные логи для лучшей индексации
            _logger.LogInformation(
                "Passenger {PassengerName} (ID: {DocumentId}) registered for flight {FlightId} with passenger ID {PassengerId}",
                passenger.FullName, passenger.DocumentId, _flightId, passengerId);

            // Success result - операция выполнена успешно
            return Result<PassengerId>.Success(passengerId);
        }
        catch (Exception ex)
        {
            // Логирование ошибки с контекстом
            _logger.LogError(ex, "Failed to register passenger {PassengerName} for flight {FlightId}",
                passenger.FullName, _flightId);
            
            // Возврат ошибки вместо исключения - более предсказуемо
            return Result<PassengerId>.Failure($"Registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Отмена регистрации пассажира
    /// </summary>
    public async Task<Result<bool>> UnregisterPassengerAsync(PassengerId passengerId)
    {
        try
        {
            // TryRemove - потокобезопасное удаление с получением значения
            if (_passengers.TryRemove(passengerId, out var passenger))
            {
                _logger.LogInformation(
                    "Passenger {PassengerName} (ID: {PassengerId}) unregistered from flight {FlightId}",
                    passenger.FullName, passengerId, _flightId);
                
                return Result<bool>.Success(true);
            }

            // Пассажир не найден - не ошибка, а бизнес-ситуация
            return Result<bool>.Failure($"Passenger with ID {passengerId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister passenger {PassengerId} from flight {FlightId}",
                passengerId, _flightId);
            return Result<bool>.Failure($"Unregistration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Смена пилота с синхронизацией и событиями
    /// </summary>
    public async Task<Result<bool>> ChangePilotAsync(IPerson newPilot)
    {
        ArgumentNullException.ThrowIfNull(newPilot);

        try
        {
            IPerson previousPilot;
            
            // Критическая секция для атомарной смены пилота
            lock (_pilotLock)
            {
                previousPilot = _pilot;    // Сохраняем предыдущего для события
                _pilot = newPilot;         // Атомарная смена
            }

            // Доменное событие о смене пилота
            var domainEvent = new PilotChangedEvent(
                DateTime.UtcNow, _flightId, previousPilot, newPilot);

            await _eventPublisher.PublishAsync(domainEvent);

            _logger.LogInformation(
                "Pilot changed from {PreviousPilot} to {NewPilot} for flight {FlightId}",
                previousPilot.FullName, newPilot.FullName, _flightId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change pilot for flight {FlightId}", _flightId);
            return Result<bool>.Failure($"Pilot change failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Обновление координат с оптимизированной синхронизацией
    /// </summary>
    public async Task<Result<Coordinate>> UpdateCoordinateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Асинхронное получение новых координат
            var newCoordinate = await _navigationService.GetCurrentCoordinateAsync(cancellationToken);
            
            // Валидация через Value Object
            if (!newCoordinate.IsValid)
            {
                return Result<Coordinate>.Failure($"Invalid coordinate: {newCoordinate}");
            }

            Coordinate? previousCoordinate;

            // Write lock - эксклюзивный доступ для записи
            _coordinateLock.EnterWriteLock();
            try
            {
                previousCoordinate = _currentCoordinate;
                
                // Проверяем, нужно ли обновление
                if (!previousCoordinate.Equals(newCoordinate))
                {
                    _currentCoordinate = newCoordinate; // Обновляем только при изменении
                }
                else
                {
                    return Result<Coordinate>.Success(newCoordinate); // Без изменений
                }
            }
            finally
            {
                _coordinateLock.ExitWriteLock(); // Обязательное освобождение блокировки
            }

            // Событие об обновлении координат
            var domainEvent = new CoordinateUpdatedEvent(
                DateTime.UtcNow, _flightId, previousCoordinate, newCoordinate);

            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            // Debug level - детальная информация для разработки
            _logger.LogDebug(
                "Coordinate updated for flight {FlightId} from {Previous} to {Current}",
                _flightId, previousCoordinate, newCoordinate);

            return Result<Coordinate>.Success(newCoordinate);
        }
        catch (OperationCanceledException)
        {
            // Отмена операции - нормальная ситуация, не ошибка
            _logger.LogInformation("Coordinate update cancelled for flight {FlightId}", _flightId);
            throw; // Пробрасываем дальше для обработки на верхнем уровне
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update coordinate for flight {FlightId}", _flightId);
            return Result<Coordinate>.Failure($"Coordinate update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Запуск рейса - основная бизнес-операция
    /// </summary>
    public async Task<Result<bool>> StartFlightAsync(CancellationToken cancellationToken = default)
    {
        // Бизнес-правило: нельзя запустить рейс без пассажиров
        if (!_passengers.Any())
        {
            return Result<bool>.Failure("Cannot start flight without passengers");
        }

        try
        {
            // Снапшоты состояния на момент запуска - immutable данные
            var currentPassengers = Passengers;           // ReadOnly коллекция
            var currentPilot = _pilot;                    // volatile читается атомарно
            var startingCoordinate = CurrentCoordinate;   // Потокобезопасное чтение

            // Сохранение в репозиторий
            await _flightRepository.SaveFlightAsync(_flightId, currentPilot, currentPassengers, cancellationToken);

            // Публикация доменного события
            var domainEvent = new FlightStartedEvent(
                DateTime.UtcNow, _flightId, currentPilot, currentPassengers, startingCoordinate);

            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

            // Информационный лог об успешном запуске
            _logger.LogInformation(
                "Flight {FlightId} started successfully with pilot {PilotName} and {PassengerCount} passengers at coordinate {Coordinate}",
                _flightId, currentPilot.FullName, currentPassengers.Count, startingCoordinate);

            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Flight start cancelled for flight {FlightId}", _flightId);
            throw; // Отмена - нормальная ситуация
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start flight {FlightId}", _flightId);
            return Result<bool>.Failure($"Flight start failed: {ex.Message}");
        }
    }

    // ========================================================================
    // RESOURCE CLEANUP - Правильное освобождение ресурсов
    // ========================================================================
    
    /// <summary>
    /// Cleanup неуправляемых ресурсов
    /// </summary>
    public void Dispose()
    {
        // ?. - safe navigation, не вызываем метод если объект null
        _coordinateLock?.Dispose(); // ReaderWriterLockSlim нужно явно освободить
    }
}

// ============================================================================
// EXTENSION METHODS - Расширение функциональности без наследования
// ============================================================================

/// <summary>
/// Extension methods для удобства работы с Plane
/// static class - нельзя создать экземпляр, только статические методы
/// </summary>
public static class PlaneExtensions
{
    /// <summary>
    /// Массовая регистрация пассажиров
    /// this Plane plane - делает метод extension method
    /// </summary>
    public static async Task<Result<bool>> RegisterMultiplePassengersAsync(
        this Plane plane,                                    // Extension target
        IEnumerable<IPerson> passengers,                     // Коллекция пассажиров
        CancellationToken cancellationToken = default)      // Отмена операции
    {
        // LINQ + async - параллельная регистрация всех пассажиров
        var tasks = passengers.Select(p => plane.RegisterPassengerAsync(p));
        
        // Ждем завершения всех задач
        var results = await Task.WhenAll(tasks);

        // Фильтруем неуспешные результаты
        var failures = results.Where(r => r.IsFailure).ToArray();
        
        if (failures.Any())
        {
            // Объединяем все ошибки в одну
            var errors = string.Join("; ", failures.Select(f => f.Error));
            return Result<bool>.Failure($"Some registrations failed: {errors}");
        }

        return Result<bool>.Success(true);
    }
}

/*
============================================================================
USAGE EXAMPLE - Пример использования с DI
============================================================================

// В Startup.cs или Program.cs для настройки DI
services.AddScoped<INavigationService, NavigationService>();
services.AddScoped<IFlightRepository, FlightRepository>();  
services.AddScoped<IFlightEventPublisher, FlightEventPublisher>();

// Использование в коде
var pilot = new Person("P123456", "John", "Smith");
var plane = new Plane(
    new FlightId(1001),           // Strong-typed ID
    pilot,                        // Immutable pilot
    navigationService,            // DI зависимость
    flightRepository,             // DI зависимость
    eventPublisher,              // DI зависимость
    logger);                     // DI зависимость

// Создание пассажиров
var passenger1 = new Person("DOC001", "Alice", "Johnson");
var passenger2 = new Person("DOC002", "Bob", "Williams");

// Result pattern использование
var registrationResult = await plane.RegisterPassengerAsync(passenger1);
if (registrationResult.IsSuccess)
{
    Console.WriteLine($"Passenger registered with ID: {registrationResult.Value}");
}
else
{
    Console.WriteLine($"Registration failed: {registrationResult.Error}");
}

// Запуск рейса
var startResult = await plane.StartFlightAsync();
if (startResult.IsSuccess)
{
    Console.WriteLine("Flight started successfully!");
}
*/
