# Брокеры сообщений для .NET разработчика: Apache Kafka и RabbitMQ

## 1. Введение в брокеры сообщений

### Что такое брокер сообщений?
Брокер сообщений (Message Broker) — это промежуточное программное обеспечение, которое обеспечивает асинхронную связь между различными частями системы через передачу сообщений. Вместо прямых вызовов API одного сервиса к другому, сервисы отправляют сообщения в брокер, который затем доставляет их получателям.

### Зачем нужны брокеры сообщений?

**Проблемы синхронной связи:**
- Если сервис-получатель недоступен, отправитель не может работать
- Высокая связанность между сервисами
- Сложность обработки пиковых нагрузок

**Преимущества асинхронной связи через брокер:**
- **Временная развязка** - отправитель может работать даже если получатель недоступен
- **Буферизация** - брокер может хранить сообщения до их обработки
- **Масштабируемость** - легко добавлять новых получателей
- **Надежность** - встроенные механизмы обработки ошибок

### Основные модели доставки сообщений

1. **At-most-once** - сообщение доставляется максимум один раз (возможна потеря)
2. **At-least-once** - сообщение доставляется минимум один раз (возможны дубликаты)
3. **Exactly-once** - сообщение доставляется ровно один раз (самый сложный режим)

## 2. Apache Kafka

### Архитектура Kafka

#### Основные компоненты:

**Topic (Топик)** - категория сообщений, логический канал для определенного типа данных
```
Пример топиков: "user-registrations", "order-events", "payment-notifications"
```

**Partition (Партиция)** - физическое разделение топика для масштабирования
- Топик может содержать несколько партиций
- Сообщения в рамках одной партиции упорядочены
- Партиции распределяются по разным брокерам (серверам)

**Producer (Продюсер)** - приложение, которое отправляет сообщения
**Consumer (Консьюмер)** - приложение, которое читает сообщения
**Consumer Group** - группа консьюмеров, работающих совместно

#### Ключевые принципы Kafka:

1. **Pull-модель** - консьюмеры сами запрашивают сообщения у брокера
2. **Immutable log** - сообщения не изменяются после записи
3. **Offset-based** - позиция чтения отслеживается через offset (номер сообщения)

### Партиционирование и порядок сообщений

**Критически важно понимать:**
- Порядок гарантируется только в рамках одной партиции
- Сообщения с одинаковым ключом попадают в одну партицию
- Если нужен строгий порядок для группы сообщений - используйте один ключ

```csharp
// Все сообщения для заказа 123 попадут в одну партицию
producer.Produce("orders", new Message<string, string> 
{ 
    Key = "order-123",  // Ключ партиционирования
    Value = orderJson 
});
```

**Проблема из теста:** Если количество партиций увеличилось после начала работы системы, сообщения с тем же ключом могут начать попадать в другие партиции, нарушив порядок.

### Consumer Groups и Rebalancing

**Consumer Group** - механизм распределения нагрузки:
- Каждая партиция читается только одним консьюмером в группе
- При добавлении/удалении консьюмера происходит rebalancing
- Количество консьюмеров не должно превышать количество партиций

**Rebalancing** - перераспределение партиций между консьюмерами:
- Происходит при изменении состава группы
- На время rebalancing обработка останавливается
- Современные версии поддерживают Incremental Cooperative Rebalancing

### Надежность и репликация

**Replication Factor** - количество копий каждой партиции
```
replication.factor=3  // Каждая партиция хранится на 3 брокерах
```

**In-Sync Replicas (ISR)** - реплики, синхронизированные с лидером
```
min.insync.replicas=2  // Минимум 2 реплики должны подтвердить запись
```

**Acks** - уровень подтверждения записи:
- `acks=0` - не ждать подтверждения (максимальная производительность)
- `acks=1` - ждать подтверждения от лидера
- `acks=all` - ждать от всех ISR (максимальная надежность)

### Идемпотентность в Kafka

```csharp
var config = new ProducerConfig
{
    EnableIdempotence = true  // Предотвращает дубликаты при повторных отправках
};
```

Как работает:
- Kafka присваивает каждому продюсеру уникальный Producer ID
- Каждое сообщение получает sequence number
- Брокер отбрасывает дубликаты с тем же Producer ID и sequence number

### Транзакции в Kafka

Для exactly-once семантики:

```csharp
using var producer = new ProducerBuilder<string, string>(config)
    .SetTransactionsTimeout(TimeSpan.FromSeconds(30))
    .Build();

producer.InitTransactions(TimeSpan.FromSeconds(30));

try 
{
    producer.BeginTransaction();
    
    // Отправка сообщений в разные топики
    await producer.ProduceAsync("payments", paymentMessage);
    await producer.ProduceAsync("analytics", analyticsMessage);
    
    // Сохранение offset'а в той же транзакции (для exactly-once)
    producer.SendOffsetsToTransaction(offsets, consumerGroupMetadata, TimeSpan.FromSeconds(30));
    
    producer.CommitTransaction(TimeSpan.FromSeconds(30));
}
catch 
{
    producer.AbortTransaction(TimeSpan.FromSeconds(30));
    throw;
}
```

### Log Compaction

Механизм для топиков, хранящих состояние (например, настройки пользователей):
- Сохраняется только последнее сообщение для каждого ключа
- Сообщение со значением `null` - это "tombstone" (удаляет все предыдущие записи с этим ключом)

### Важные параметры консьюмера

**max.poll.interval.ms** - максимальное время между вызовами poll():
- Если превышено, консьюмер считается мертвым
- Инициируется rebalancing

**session.timeout.ms** - таймаут сессии консьюмера
**heartbeat.interval.ms** - интервал отправки heartbeat

### Пример настройки Kafka Consumer в .NET

```csharp
var config = new ConsumerConfig
{
    GroupId = "my-consumer-group",
    BootstrapServers = "localhost:9092",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,  // Ручное управление offset'ами
    MaxPollIntervalMs = 300000,  // 5 минут
    SessionTimeoutMs = 30000,    // 30 секунд
    HeartbeatIntervalMs = 3000   // 3 секунды
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("my-topic");

try 
{
    while (true)
    {
        var result = consumer.Consume(cancellationToken);
        
        await ProcessMessageAsync(result.Message.Value);
        
        // ВАЖНО: коммит только после успешной обработки
        consumer.Commit(result);
    }
}
catch (OperationCanceledException)
{
    consumer.Close();
}
```

## 3. RabbitMQ

### Архитектура RabbitMQ

RabbitMQ основан на протоколе AMQP и использует другую модель:

#### Основные компоненты:

**Exchange (Эксчейндж)** - маршрутизатор сообщений
**Queue (Очередь)** - хранилище сообщений
**Binding (Привязка)** - правило маршрутизации от эксчейнджа к очереди
**Producer** - отправитель сообщений
**Consumer** - получатель сообщений

#### Типы Exchange'ей:

1. **Direct** - точное соответствие routing key
2. **Topic** - паттерн-matching с wildcards (* и #)
3. **Fanout** - широковещание (всем привязанным очередям)
4. **Headers** - маршрутизация по заголовкам

### Push vs Pull модель

**RabbitMQ использует Push-модель:**
- Брокер активно "проталкивает" сообщения консьюмерам
- Можно настроить Prefetch Count для управления потоком

```csharp
channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
```

### Надежность в RabbitMQ

#### Publisher Confirms
Механизм подтверждения доставки сообщений:

```csharp
channel.ConfirmSelect();  // Включение режима подтверждений
channel.BasicPublish(exchange, routingKey, properties, body);

if (channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
{
    // Сообщение доставлено
}
else
{
    // Ошибка доставки
}
```

#### Mandatory флаг
Возврат немаршрутизированных сообщений:

```csharp
channel.BasicPublish(
    exchange: "my-exchange", 
    routingKey: "wrong-key", 
    mandatory: true,  // Вернуть, если не удалось маршрутизировать
    properties: null, 
    body: message);

// Обработка возвращенных сообщений
channel.BasicReturn += (sender, args) => 
{
    // Сообщение не было доставлено
    Console.WriteLine($"Message returned: {args.ReplyText}");
};
```

#### Durability (Долговечность)
Для сохранения данных при перезапуске брокера:
- Очередь: `durable: true`
- Сообщение: `delivery_mode: 2` (persistent)

```csharp
// Создание долговечной очереди
channel.QueueDeclare(
    queue: "durable-queue",
    durable: true,      // Очередь переживет перезапуск брокера
    exclusive: false,
    autoDelete: false);

// Отправка персистентного сообщения
var properties = channel.CreateBasicProperties();
properties.Persistent = true;  // delivery_mode = 2

channel.BasicPublish("", "durable-queue", properties, body);
```

### Обработка ошибок и повторы

#### Basic Ack/Nack
```csharp
public void HandleMessage(BasicDeliverEventArgs eventArgs)
{
    try
    {
        ProcessMessage(eventArgs.Body.ToArray());
        channel.BasicAck(eventArgs.DeliveryTag, false);
    }
    catch (TemporaryException)
    {
        // Вернуть сообщение в очередь для повторной попытки
        channel.BasicNack(eventArgs.DeliveryTag, false, requeue: true);
    }
    catch (Exception)
    {
        // Отклонить навсегда (пошлет в DLX если настроен)
        channel.BasicNack(eventArgs.DeliveryTag, false, requeue: false);
    }
}
```

**Проблема бесконечного цикла:**
Сообщение с `requeue: true` возвращается в начало очереди и сразу обрабатывается снова. Без дополнительной логики это создает бесконечный цикл.

**Решения:**
- Использование Dead Letter Exchange (DLX)
- Экспоненциальные задержки
- Счетчик попыток в заголовках сообщения

#### Dead Letter Exchange (DLX)

```csharp
// Настройка очереди с DLX
var arguments = new Dictionary<string, object>
{
    {"x-dead-letter-exchange", "dlx-exchange"},
    {"x-dead-letter-routing-key", "failed"},
    {"x-message-ttl", 60000}  // TTL 60 секунд
};

channel.QueueDeclare("main-queue", true, false, false, arguments);
```

### Маршрутизация в Topic Exchange

Для чат-приложения с ключами вида `chats.<chat-id>.<from-user>.<to-user>`:

```csharp
// Binding key для получения всех сообщений пользователю user-B
channel.QueueBind("user-B-queue", "chat-exchange", "chats.*.*.user-B");
```

Wildcards:
- `*` - заменяет ровно одно слово
- `#` - заменяет ноль или более слов

### Lazy Queues

Для хранения миллионов сообщений:

```csharp
var arguments = new Dictionary<string, object>
{
    {"x-queue-mode", "lazy"}  // Сообщения сразу записываются на диск
};

channel.QueueDeclare("lazy-queue", durable: true, false, false, arguments);
```

### Потокобезопасность в RabbitMQ .NET Client

**ВАЖНО:** `IModel` (канал) НЕ потокобезопасен!

```csharp
// НЕПРАВИЛЬНО - один канал для нескольких потоков
public class BadMessageHandler
{
    private readonly IModel _channel;  // Один канал для всех потоков - ОПАСНО!
    
    public void Process(BasicDeliverEventArgs args)
    {
        // Несколько потоков вызывают BasicAck на одном канале
        _channel.BasicAck(args.DeliveryTag, false);  // Race condition!
    }
}

// ПРАВИЛЬНО - отдельный канал для каждого потока
public class GoodMessageHandler
{
    private readonly IConnection _connection;
    private readonly ThreadLocal<IModel> _channel;
    
    public GoodMessageHandler(IConnection connection)
    {
        _connection = connection;
        _channel = new ThreadLocal<IModel>(() => _connection.CreateModel());
    }
    
    public void Process(BasicDeliverEventArgs args)
    {
        _channel.Value.BasicAck(args.DeliveryTag, false);
    }
}
```

### Prefetch и Head-of-Line Blocking

```csharp
// Настройка prefetch
channel.BasicQos(prefetchSize: 0, prefetchCount: 50, global: false);
```

Если один из 50 сообщений в буфере "завис" на обработке, остальные 49 сообщений будут ждать - это называется Head-of-Line Blocking.

### Alternate Exchange vs Dead Letter Exchange

**Alternate Exchange (AE):**
- Обрабатывает сообщения, которые не удалось маршрутизировать (unroutable)
- Настраивается на уровне exchange'а

**Dead Letter Exchange (DLX):**
- Обрабатывает сообщения, отклоненные консьюмером или истекшие по TTL
- Настраивается на уровне очереди

### RabbitMQ Streams

Новая функциональность, похожая на Kafka:
- Immutable log сообщений
- Offset-based чтение
- Возможность replay сообщений
- Несколько консьюмеров с независимыми офсетами

## 4. Паттерны и лучшие практики

### Transactional Outbox Pattern

Решает проблему одновременного сохранения данных в БД и отправки сообщений:

```csharp
// В одной транзакции БД
using var transaction = dbContext.Database.BeginTransaction();

// 1. Сохраняем бизнес-данные
dbContext.Orders.Add(order);

// 2. Сохраняем сообщение в таблицу Outbox
dbContext.OutboxMessages.Add(new OutboxMessage 
{
    MessageId = Guid.NewGuid(),
    Topic = "order-created",
    Payload = JsonSerializer.Serialize(order),
    CreatedAt = DateTime.UtcNow
});

await dbContext.SaveChangesAsync();
await transaction.CommitAsync();

// Отдельный процесс (Relay) читает из Outbox и отправляет в брокер
```

### Claim Check Pattern

Для сообщений размером > 1MB:

```csharp
// Большие данные сохраняем отдельно
var blobId = await blobStorage.SaveAsync(largePayload);

// В брокер отправляем только ссылку
var message = new ClaimCheckMessage 
{
    BlobId = blobId,
    Size = largePayload.Length,
    Checksum = ComputeChecksum(largePayload)
};
```

### Идемпотентность обработчиков

```csharp
public async Task Handle(Message message)
{
    // Проверяем, не обработано ли уже
    if (await _processedMessages.ContainsAsync(message.Id))
        return;
        
    // ПРОБЛЕМА: если упадем здесь, сообщение будет обработано повторно
    await ProcessBusinessLogic(message.Data);
    await _processedMessages.AddAsync(message.Id);
}

// РЕШЕНИЕ: используем транзакции или сохраняем ID до обработки
public async Task Handle(Message message)
{
    using var transaction = _dbContext.Database.BeginTransaction();
    
    if (await _processedMessages.ContainsAsync(message.Id))
        return;
        
    await _processedMessages.AddAsync(message.Id);  // Сначала ID
    await ProcessBusinessLogic(message.Data);       // Потом обработка
    
    await transaction.CommitAsync();
}
```

### Saga Pattern: Choreography vs Orchestration

**Choreography (Хореография):**
- Сервисы обмениваются событиями напрямую
- Децентрализованное управление
- Подходит для простых, линейных процессов

**Orchestration (Оркестрация):**
- Центральный координатор управляет процессом
- Подходит для сложных бизнес-процессов с ветвлениями
- Упрощает мониторинг и отладку

### Обработка ошибок с Polly

```csharp
var retryPolicy = Policy
    .Handle<TemporaryException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
            TimeSpan.FromMilliseconds(Random.Next(0, 1000)),  // Jitter для предотвращения thundering herd
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            _logger.LogWarning($"Retry {retryCount} after {timespan} delay");
        });

await retryPolicy.ExecuteAsync(async () =>
{
    await CallExternalService();
});
```

### Distributed Tracing

Для отслеживания сообщений через систему:

```csharp
public class MessageWithTracing
{
    public string CorrelationId { get; set; }  // Сквозной ID операции
    public string CausationId { get; set; }    // ID сообщения-причины
    public string MessageId { get; set; }      // Уникальный ID сообщения
    public object Data { get; set; }
}

// При создании нового сообщения в ответ на полученное
var responseMessage = new MessageWithTracing
{
    CorrelationId = incomingMessage.CorrelationId,  // Сохраняем
    CausationId = incomingMessage.MessageId,        // Указываем причину
    MessageId = Guid.NewGuid(),                     // Новый ID
    Data = processedData
};
```

## 5. Мониторинг и отладка

### Kafka Metrics
- Consumer Lag - отставание консьюмера от последних сообщений
- Throughput - количество сообщений в секунду
- Error Rate - процент ошибок обработки

### RabbitMQ Management
- Queue depth - количество сообщений в очереди
- Consumer utilisation - загруженность консьюмеров
- Publishing/Delivery rates

### Logging Best Practices
```csharp
_logger.LogInformation("Processing message {MessageId} with correlation {CorrelationId}", 
    message.Id, message.CorrelationId);

try 
{
    await ProcessMessage(message);
    _logger.LogInformation("Successfully processed message {MessageId}", message.Id);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process message {MessageId}", message.Id);
    throw;
}
```

## 6. Конфигурация для production

### Kafka Production Settings

**Producer:**
```csharp
var config = new ProducerConfig
{
    BootstrapServers = "kafka1:9092,kafka2:9092,kafka3:9092",
    Acks = Acks.All,
    Retries = int.MaxValue,
    MaxInFlight = 5,
    EnableIdempotence = true,
    CompressionType = CompressionType.Lz4,
    LingerMs = 5,
    BatchSize = 32768
};
```

**Consumer:**
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "kafka1:9092,kafka2:9092,kafka3:9092",
    GroupId = "my-service-v1.0",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,
    MaxPollIntervalMs = 300000,
    SessionTimeoutMs = 30000,
    HeartbeatIntervalMs = 3000,
    FetchMinBytes = 1,
    FetchMaxWaitMs = 500
};
```

### RabbitMQ Production Settings

```csharp
var factory = new ConnectionFactory()
{
    HostName = "rabbitmq.company.com",
    UserName = "app-user",
    Password = "secure-password",
    VirtualHost = "/production",
    Port = 5672,
    RequestedHeartbeat = TimeSpan.FromSeconds(60),
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    AutomaticRecoveryEnabled = true,
    TopologyRecoveryEnabled = true,
    RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
};

// Настройка кластера
factory.HostName = "rabbitmq1.company.com,rabbitmq2.company.com,rabbitmq3.company.com";
```

## Заключение

Понимание этих концепций поможет вам уверенно отвечать на вопросы о брокерах сообщений. Ключевые моменты для запоминания:

1. **Kafka** - высокопроизводительный лог сообщений с pull-моделью
2. **RabbitMQ** - гибкий брокер с push-моделью и богатыми возможностями маршрутизации
3. **Надежность** достигается через репликацию, подтверждения и правильную обработку ошибок
4. **Порядок сообщений** - критически важная тема в обеих системах
5. **Идемпотентность** и **exactly-once** семантика требуют дополнительных усилий
6. **Мониторинг** и **observability** критически важны в production

Изучите также практические примеры интеграции с .NET через NuGet пакеты:
- **Kafka**: `Confluent.Kafka`
- **RabbitMQ**: `RabbitMQ.Client`, `MassTransit`, `EasyNetQ`
