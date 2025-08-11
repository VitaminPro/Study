# Подготовка к собеседованию .NET разработчика в Ozon
## Диалог скрининг-интервью (2 часа)

**Speaker 1 (Интервьюер):** Добрый день! Спасибо, что пришли на собеседование на позицию .NET разработчика в Ozon. Это будет скрининг-интервью, которое обычно длится около двух часов. Мы обсудим ваш опыт, технические знания по .NET, асинхронности, базам данных, паттернам проектирования и брокерам сообщений. Расскажите кратко о себе и вашем опыте с .NET.

**Speaker 2 (Кандидат):** Добрый день! Меня зовут Александр, у меня 8 лет опыта в разработке на .NET. Начинал с .NET Framework 4.x, последние 4 года работаю с .NET Core/.NET 6+. Разрабатывал высоконагруженные веб-сервисы, REST API на ASP.NET Core, интегрировал с различными БД включая PostgreSQL и MongoDB, работал с брокерами сообщений RabbitMQ и Apache Kafka. В последнем проекте занимался архитектурой микросервисов и оптимизацией производительности асинхронного кода.

---

## Блок 1: Управление памятью и сборка мусора

**Speaker 1:** Отлично! Давайте начнем с основ. Расскажите про стек и кучу в .NET. В чем их принципиальные различия?

**Speaker 2:** В .NET память разделена на стек и кучу. Стек используется для хранения локальных переменных значимых типов, параметров методов и адресов возврата. Он работает по принципу LIFO и автоматически очищается при выходе из области видимости. Куча используется для ссылочных типов и больших значимых типов. 

Ключевые различия: стек быстрее в доступе, но ограничен по размеру (~1-8MB), куча медленнее, но практически безгранична. Стек управляется автоматически, куча требует сборки мусора.

**Speaker 1:** Хорошо. А как работает Garbage Collector в .NET? Расскажите про поколения.

**Speaker 2:** GC в .NET использует поколенческий алгоритм с тремя поколениями:
- Generation 0: новые объекты, собирается часто и быстро
- Generation 1: объекты, пережившие одну сборку
- Generation 2: долгоживущие объекты, собирается редко

GC работает по принципу "mark-and-sweep": сначала отмечает достижимые объекты, затем удаляет недостижимые. Есть также Large Object Heap для объектов >85KB, который собирается только вместе с Generation 2.

В .NET Core добавились Server GC (для многопоточных приложений) и Workstation GC (для клиентских), а также конкурентная сборка мусора для минимизации пауз.

**Speaker 1:** Что такое финализатор и когда его использовать? В чем разница с IDisposable?

**Speaker 2:** Финализатор (деструктор в C#) - это метод ~ClassName(), который вызывается GC перед удалением объекта. Он нужен для освобождения неуправляемых ресурсов, но имеет серьезные недостатки:
- Непредсказуемое время вызова
- Объекты с финализатором живут дольше (минимум два цикла GC)
- Снижает производительность

IDisposable - это паттерн для детерминированного освобождения ресурсов. Метод Dispose() вызывается явно или автоматически через using. Лучшая практика - реализовывать оба паттерна через Dispose Pattern:

```csharp
public class ResourceHolder : IDisposable
{
    private bool disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Освобождение управляемых ресурсов
            }
            // Освобождение неуправляемых ресурсов
            disposed = true;
        }
    }
    
    ~ResourceHolder()
    {
        Dispose(false);
    }
}
```

---

## Блок 2: Коллекции в .NET

**Speaker 1:** Перейдем к коллекциям. Расскажите про Array, List<T>, Dictionary<T,K> - их особенности и сложность операций.

**Speaker 2:** **Array:**
- Фиксированный размер, элементы хранятся непрерывно в памяти
- Доступ по индексу: O(1)
- Поиск: O(n) для неотсортированного, O(log n) для отсортированного
- Вставка/удаление: невозможно изменить размер

**List<T>:**
- Динамический массив, основан на Array
- Доступ по индексу: O(1)
- Добавление в конец: амортизированно O(1), в худшем случае O(n) при расширении
- Вставка/удаление в середине: O(n)
- Поиск: O(n), для отсортированного есть BinarySearch O(log n)

**Dictionary<T,K>:**
- Хеш-таблица с разрешением коллизий через цепочки
- Поиск, вставка, удаление: среднее O(1), худший случай O(n)
- В .NET Core используется Robin Hood hashing для лучшей производительности
- Порядок элементов не гарантирован (до .NET Core), в новых версиях сохраняется порядок вставки

**Speaker 1:** Какие еще коллекции знаете? Когда использовать ConcurrentDictionary?

**Speaker 2:** Из других важных коллекций:
- **HashSet<T>** - множество, быстрый поиск уникальных элементов O(1)
- **LinkedList<T>** - двусвязный список, O(1) вставка/удаление при наличии узла
- **Queue<T>** и **Stack<T>** - FIFO и LIFO соответственно
- **SortedDictionary<T,K>** - красно-черное дерево, O(log n) операции, сохраняет порядок

**ConcurrentDictionary<T,K>** использую для потокобезопасных операций:
```csharp
// Вместо lock + Dictionary
private readonly ConcurrentDictionary<string, User> _cache = new();

// Атомарные операции
_cache.TryAdd(key, value);
_cache.TryGetValue(key, out var result);
_cache.AddOrUpdate(key, value, (k, v) => newValue);
```

Он использует lock-free алгоритмы для чтения и минимальные блокировки для записи, что дает отличную производительность в многопоточной среде.

---

## Блок 3: Асинхронное программирование

**Speaker 1:** Отлично. Теперь поговорим об асинхронности. Объясните async/await - как это работает под капотом?

**Speaker 2:** async/await - это синтаксический сахар над Task-based Asynchronous Pattern. Под капотом компилятор создает state machine:

```csharp
public async Task<string> GetDataAsync()
{
    var result = await httpClient.GetStringAsync(url); // Точка ожидания
    return result.ToUpper();
}
```

Компилятор преобразует это в класс, реализующий IAsyncStateMachine. В точке await:
1. Метод проверяет, завершена ли операция
2. Если нет - настраивает continuation и возвращает управление
3. При завершении операции continuation возобновляет выполнение

Важные моменты:
- async метод возвращает Task сразу, не блокируя поток
- await "распаковывает" Task и возвращает результат
- ConfigureAwait(false) предотвращает захват SynchronizationContext
- В библиотечном коде всегда использую ConfigureAwait(false)

**Speaker 1:** Что происходит с потоками при async/await? Почему это эффективнее обычной многопоточности?

**Speaker 2:** Ключевое отличие - async/await не создает новые потоки. Вместо блокировки потока при I/O операциях, поток возвращается в ThreadPool и может обработать другие запросы.

Пример с обычной синхронизацией:
```csharp
// Плохо - блокирует поток на 1 секунду
Thread.Sleep(1000); // Поток заблокирован
```

С асинхронностью:
```csharp
// Хорошо - поток освобождается
await Task.Delay(1000); // Поток возвращается в пул
```

Преимущества:
- **Масштабируемость**: один ThreadPool может обслужить тысячи одновременных операций
- **Экономия ресурсов**: не создаем лишние потоки (каждый поток ~1MB памяти)
- **Отзывчивость UI**: в десктопных приложениях UI поток не блокируется

В веб-приложениях это критично - вместо 200 потоков для 200 запросов, используем 8-16 потоков ThreadPool.

**Speaker 1:** Расскажите про примитивы синхронизации: lock, Mutex, Semaphore. Когда какой использовать?

**Speaker 2:** **lock (Monitor):**
- Самый быстрый, работает только внутри одного процесса
- Обеспечивает взаимное исключение для критических секций
```csharp
private readonly object _lockObject = new();
lock (_lockObject)
{
    // Критическая секция
}
```

**Mutex:**
- Межпроцессная синхронизация, именованный или безымянный
- Медленнее lock, но работает между процессами
```csharp
using var mutex = new Mutex(false, "Global\\MyApp");
if (mutex.WaitOne(TimeSpan.FromSeconds(10)))
{
    try { /* работа */ }
    finally { mutex.ReleaseMutex(); }
}
```

**Semaphore:**
- Ограничивает количество потоков, имеющих доступ к ресурсу
- Полезен для ограничения нагрузки
```csharp
private static readonly SemaphoreSlim semaphore = new(3, 3); // Максимум 3 потока

await semaphore.WaitAsync();
try
{
    // Работа с ресурсом
}
finally
{
    semaphore.Release();
}
```

**Для async кода использую:**
- SemaphoreSlim вместо Semaphore
- async версии методов (WaitAsync)
- избегаю lock в async методах (deadlock risk)

**Speaker 1:** А что с Task.Run, когда его использовать в веб-приложениях?

**Speaker 2:** Task.Run в веб-приложениях нужно использовать очень осторожно. Он создает новую задачу в ThreadPool, что может ухудшить производительность.

**Плохое использование:**
```csharp
// Не делайте так в контроллерах!
public async Task<IActionResult> Get()
{
    var result = await Task.Run(() => SomeMethod()); // Лишний переход между потоками
    return Ok(result);
}
```

**Хорошие сценарии для Task.Run:**
1. **CPU-интенсивные операции** в otherwise async контексте:
```csharp
public async Task<int> CalculateAsync(int[] data)
{
    // Освобождаем основной поток от CPU работы
    return await Task.Run(() => data.Sum(x => x * x));
}
```

2. **Преобразование синхронного API** в асинхронный:
```csharp
// Legacy библиотека без async методов
public Task<byte[]> ProcessImageAsync(Stream input)
{
    return Task.Run(() => legacyImageProcessor.Process(input));
}
```

**Правило:** используйте Task.Run только когда действительно нужно перенести CPU работу на background поток. Для I/O операций используйте нативные async методы.

---

## Блок 4: Базы данных

**Speaker 1:** Перейдем к базам данных. Что такое индексы и как они работают? Какие типы индексов знаете?

**Speaker 2:** Индекс - это структура данных, которая ускоряет поиск записей в таблице. Принцип работы похож на алфавитный указатель в книге.

**Основные типы индексов:**

**Кластерный индекс:**
- Физически упорядочивает данные в таблице
- Один на таблицу (обычно по первичному ключу)
- Поиск O(log n), но вставка может быть медленной из-за реорганизации

**Некластерный индекс:**
- Отдельная структура, указывающая на строки данных
- Может быть несколько на таблице
- Быстрый поиск, но два обращения к диску (индекс → данные)

**Композитный индекс:**
```sql
CREATE INDEX IX_User_LastName_FirstName ON Users (LastName, FirstName);
```
Эффективен для поиска по левым колонкам (LastName) или по обеим, но не по правым (только FirstName).

**Практические советы:**
- Индексируйте колонки в WHERE, JOIN, ORDER BY
- Избегайте избыточных индексов (замедляют INSERT/UPDATE/DELETE)
- Мониторьте использование индексов через query execution plans

**Speaker 1:** Расскажите про уровни изоляций транзакций. Какие проблемы они решают?

**Speaker 2:** Уровни изоляций решают проблемы параллельного доступа к данным:

**Проблемы параллелизма:**
1. **Dirty Read** - чтение незафиксированных изменений
2. **Non-repeatable Read** - изменение данных между чтениями в одной транзакции
3. **Phantom Read** - появление новых строк, соответствующих условию

**Уровни изоляции:**

**READ UNCOMMITTED:**
- Разрешены все проблемы
- Максимальная производительность, минимальная согласованность

**READ COMMITTED (по умолчанию в SQL Server):**
- Предотвращает Dirty Read
- Читает только зафиксированные данные
```sql
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
```

**REPEATABLE READ:**
- Предотвращает Dirty Read и Non-repeatable Read
- Блокирует изменение прочитанных строк

**SERIALIZABLE:**
- Предотвращает все проблемы
- Максимальная изоляция, но может быть медленно

**В .NET работаю с изоляцией так:**
```csharp
using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
try
{
    // Выполнение операций
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

**Speaker 1:** Что такое ACID? Объясните каждое свойство.

**Speaker 2:** ACID - это четыре ключевых свойства надежных транзакций:

**Atomicity (Атомарность):**
- Транзакция выполняется полностью или не выполняется вообще
- "Все или ничего" - нет частичных изменений
```csharp
// Пример: перевод денег - либо оба UPDATE, либо ни одного
using var transaction = context.Database.BeginTransaction();
try
{
    fromAccount.Balance -= amount;
    toAccount.Balance += amount;
    await context.SaveChangesAsync();
    transaction.Commit();
}
catch
{
    transaction.Rollback(); // Отменяет ВСЕ изменения
}
```

**Consistency (Согласованность):**
- База данных остается в корректном состоянии до и после транзакции
- Соблюдаются все ограничения, правила и триггеры
- Пример: нарушение Foreign Key откатит всю транзакцию

**Isolation (Изолированность):**
- Параллельные транзакции не влияют друг на друга
- Каждая транзакция видит согласованное состояние данных

**Durability (Долговечность):**
- Зафиксированные изменения сохраняются даже при сбоях
- Обеспечивается через transaction log и механизмы восстановления

В высоконагруженных системах иногда приходится жертвовать строгим ACID ради производительности, используя eventual consistency.

---

## Блок 5: Паттерны проектирования

**Speaker 1:** Расскажите о паттернах проектирования, которые вы использовали в реальных проектах.

**Speaker 2:** Активно использую несколько паттернов:

**Repository Pattern:**
Инкапсулирует логику доступа к данным, упрощает тестирование:
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
```

**Dependency Injection:**
Основа современных .NET приложений:
```csharp
// Startup.cs
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserRepository, UserRepository>();

// Controller
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
}
```

**Strategy Pattern:**
Для выбора алгоритмов во время выполнения:
```csharp
public interface IPaymentStrategy
{
    Task<PaymentResult> ProcessAsync(decimal amount);
}

public class PaymentService
{
    private readonly Dictionary<PaymentType, IPaymentStrategy> _strategies;
    
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentType type, decimal amount)
    {
        return await _strategies[type].ProcessAsync(amount);
    }
}
```

**Command Pattern:**
Использую с MediatR для CQRS:
```csharp
public class CreateUserCommand : IRequest<User>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Логика создания пользователя
    }
}
```

**Speaker 1:** А что насчет SOLID принципов? Можете привести примеры нарушения и исправления?

**Speaker 2:** **Single Responsibility Principle (SRP):**

Нарушение:
```csharp
// Плохо - класс отвечает за несколько задач
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    
    // Логика валидации
    public bool IsValidEmail() => Email.Contains("@");
    
    // Логика сохранения
    public void SaveToDatabase() { /* ... */ }
    
    // Логика отправки email
    public void SendWelcomeEmail() { /* ... */ }
}
```

Исправление:
```csharp
// Хорошо - разделение ответственности
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class EmailValidator
{
    public bool IsValid(string email) => email.Contains("@");
}

public class UserRepository
{
    public async Task SaveAsync(User user) { /* ... */ }
}

public class EmailService
{
    public async Task SendWelcomeEmailAsync(User user) { /* ... */ }
}
```

**Open/Closed Principle (OCP):**
```csharp
// Хорошо - открыт для расширения, закрыт для модификации
public abstract class NotificationSender
{
    public abstract Task SendAsync(string message, string recipient);
}

public class EmailSender : NotificationSender
{
    public override async Task SendAsync(string message, string recipient)
    {
        // Email логика
    }
}

public class SmsSender : NotificationSender
{
    public override async Task SendAsync(string message, string recipient)
    {
        // SMS логика
    }
}
```

**Interface Segregation Principle (ISP):**
Лучше много маленьких интерфейсов, чем один большой:
```csharp
// Вместо одного большого интерфейса
public interface IUserOperations
{
    Task<User> GetAsync(int id);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<byte[]> ExportToExcelAsync();
    Task SendReportEmailAsync();
}

// Лучше разделить
public interface IUserReader
{
    Task<User> GetAsync(int id);
}

public interface IUserWriter
{
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public interface IUserReporter
{
    Task<byte[]> ExportToExcelAsync();
    Task SendReportEmailAsync();
}
```

---

## Блок 6: Брокеры сообщений

**Speaker 1:** Последняя тема - брокеры сообщений. С какими работали? Расскажите про паттерны гарантированной доставки.

**Speaker 2:** Работал с RabbitMQ, Apache Kafka и Azure Service Bus. Каждый имеет свои особенности для обеспечения надежности.

**Паттерны гарантированной доставки:**

**1. At-least-once delivery:**
Сообщение доставляется минимум один раз, возможны дубли.

RabbitMQ с подтверждениями:
```csharp
// Publisher
channel.ConfirmSelect(); // Включаем publisher confirms
channel.BasicPublish(exchange, routingKey, mandatory: true, body: message);
channel.WaitForConfirmsOrDie(); // Ждем подтверждения

// Consumer
channel.BasicConsume(queue, autoAck: false, consumer);

// В обработчике
public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, 
    bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
{
    try
    {
        ProcessMessage(body);
        channel.BasicAck(deliveryTag, false); // Подтверждаем обработку
    }
    catch (Exception ex)
    {
        channel.BasicNack(deliveryTag, false, true); // Возвращаем в очередь
    }
}
```

**2. Exactly-once delivery:**
Самый сложный паттерн, часто реализуется через идемпотентность.

```csharp
public class OrderProcessor
{
    private readonly HashSet<string> _processedMessageIds = new();
    
    public async Task ProcessOrderAsync(OrderMessage message)
    {
        // Идемпотентная обработка
        if (_processedMessageIds.Contains(message.MessageId))
        {
            return; // Уже обработано
        }
        
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Обработка заказа
            await ProcessOrder(message.Order);
            
            // Сохраняем ID обработанного сообщения
            await _context.ProcessedMessages.AddAsync(new ProcessedMessage 
            { 
                MessageId = message.MessageId, 
                ProcessedAt = DateTime.UtcNow 
            });
            
            await _context.SaveChangesAsync();
            transaction.Commit();
            
            // Подтверждаем только после успешной записи в БД
            _channel.BasicAck(message.DeliveryTag, false);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

**3. Outbox Pattern:**
Для обеспечения консистентности между БД и сообщениями:
```csharp
public class OrderService
{
    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Создаем заказ
            var order = new Order(request);
            _context.Orders.Add(order);
            
            // Добавляем сообщение в Outbox
            _context.OutboxMessages.Add(new OutboxMessage
            {
                MessageType = "OrderCreated",
                Payload = JsonSerializer.Serialize(new OrderCreatedEvent(order)),
                CreatedAt = DateTime.UtcNow
            });
            
            await _context.SaveChangesAsync();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

// Отдельный сервис публикует сообщения из Outbox
public class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _context.OutboxMessages
                .Where(m => !m.Processed)
                .Take(100)
                .ToListAsync();
                
            foreach (var message in messages)
            {
                await _messagePublisher.PublishAsync(message.MessageType, message.Payload);
                message.Processed = true;
            }
            
            await _context.SaveChangesAsync();
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

**Speaker 1:** Как обрабатываете Dead Letter Queue и retry механизмы?

**Speaker 2:** **Dead Letter Queue (DLQ)** использую для сообщений, которые не удалось обработать после нескольких попыток:

```csharp
public class ResilientMessageHandler
{
    private readonly ILogger<ResilientMessageHandler> _logger;
    private readonly int _maxRetries = 3;
    
    public async Task<bool> HandleMessageAsync(IMessage message)
    {
        var retryCount = message.GetRetryCount();
        
        try
        {
            await ProcessMessageAsync(message);
            return true;
        }
        catch (TransientException ex) when (retryCount < _maxRetries)
        {
            // Временная ошибка - retry с exponential backoff
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
            await PublishForRetryAsync(message, delay, retryCount + 1);
            
            _logger.LogWarning("Message retry {RetryCount}/{MaxRetries} after {Delay}s: {Error}", 
                retryCount + 1, _maxRetries, delay.TotalSeconds, ex.Message);
            
            return false;
        }
        catch (Exception ex)
        {
            // Критическая ошибка или превышен лимит retry
            await PublishToDeadLetterQueueAsync(message, ex);
            
            _logger.LogError(ex, "Message sent to DLQ after {RetryCount} retries", retryCount);
            
            return false;
        }
    }
    
    private async Task PublishToDeadLetterQueueAsync(IMessage message, Exception exception)
    {
        var dlqMessage = new DeadLetterMessage
        {
            OriginalMessage = message,
            FailureReason = exception.Message,
            FailedAt = DateTime.UtcNow,
            RetryCount = message.GetRetryCount()
        };
        
        await _deadLetterPublisher.PublishAsync(dlqMessage);
    }
}
```

**Мониторинг DLQ:**
- Настраиваю алерты на появление сообщений в DLQ
- Регулярный анализ причин попадания сообщений в DLQ
- Возможность ручной переобработки исправленных сообщений

**Стратегии retry:**
- **Exponential Backoff** - увеличиваем задержку: 1s, 2s, 4s, 8s
- **Jittering** - добавляем случайность, чтобы избежать thundering herd
- **Circuit Breaker** - временно прекращаем обработку при высоком проценте ошибок

**Speaker
