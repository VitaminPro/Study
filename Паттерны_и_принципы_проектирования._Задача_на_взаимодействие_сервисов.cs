/*
Паттерны и принципы проектирования. Задача на взаимодействие сервисов
Необходимо интегрировать 2 приложения, которые располагаются на разных серверах.
Приложения А регистрирует пользователя и должно отправить Registration Message по Email
Приложение B умеет отправлять Email
Расскажите о способах интеграции, протоколах и возможных проблемах выбранного вами способа интеграции

Application A
public class UserService {
     public async Task RegisterUser(UserModel user) {
	 await _validator.ValidateAndThrowAsync(user);
	 await _userRepository.CreateAsync(user);
	 await _unitOfWork.SaveChangesAsync();
	 
	 //Send email message
	 }
}

Application B
public class EmailSender {
    public void Send(string recipient, string message) {
             var smtp = new SmtpClient();
             //..
    }
}	
*/


// ========================================
// СПОСОБ 1: СИНХРОННЫЙ HTTP API ВЫЗОВ
// ========================================

// Application A - UserService с HTTP интеграцией
public class UserService 
{
    private readonly IValidator<UserModel> _validator; // Валидатор для проверки модели пользователя
    private readonly IUserRepository _userRepository; // Репозиторий для работы с пользователями
    private readonly IUnitOfWork _unitOfWork; // Unit of Work для управления транзакциями
    private readonly HttpClient _httpClient; // HTTP клиент для вызова внешнего сервиса
    private readonly EmailServiceConfiguration _emailConfig; // Конфигурация email сервиса
    private readonly ILogger<UserService> _logger; // Логгер для отслеживания событий

    public async Task RegisterUser(UserModel user) 
    {
        // Валидируем входные данные и выбрасываем исключение при ошибке
        await _validator.ValidateAndThrowAsync(user);
        
        // Сохраняем пользователя в базу данных
        await _userRepository.CreateAsync(user);
        
        // Фиксируем изменения в БД через Unit of Work
        await _unitOfWork.SaveChangesAsync();
        
        try 
        {
            // Создаем объект запроса для отправки email
            var emailRequest = new EmailRequest 
            {
                Recipient = user.Email, // Email получателя из модели пользователя
                Subject = "Welcome to our service!", // Тема письма
                Body = $"Hello {user.Name}, welcome!" // Тело письма с персонализацией
            };
            
            // Сериализуем запрос в JSON для передачи по HTTP
            var json = JsonSerializer.Serialize(emailRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Отправляем POST запрос к Application B с таймаутом
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsync(_emailConfig.EmailServiceUrl, content, cts.Token);
            
            // Проверяем успешность ответа
            response.EnsureSuccessStatusCode();
            
            // Логируем успешную отправку
            _logger.LogInformation("Email sent successfully for user {UserId}", user.Id);
        }
        catch (HttpRequestException ex)
        {
            // Логируем ошибку сети, но не прерываем регистрацию
            _logger.LogError(ex, "Failed to send email for user {UserId} due to network error", user.Id);
            // В реальном проекте здесь можно добавить в очередь повторных попыток
        }
        catch (TaskCanceledException ex)
        {
            // Логируем таймаут запроса
            _logger.LogError(ex, "Email sending timeout for user {UserId}", user.Id);
        }
    }
}

// Модель запроса для отправки email
public class EmailRequest 
{
    public string Recipient { get; set; } // Email получателя
    public string Subject { get; set; } // Тема письма
    public string Body { get; set; } // Содержимое письма
}

// Application B - EmailController для обработки HTTP запросов
[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase 
{
    private readonly EmailSender _emailSender; // Сервис отправки email
    private readonly ILogger<EmailController> _logger; // Логгер для контроллера

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request) 
    {
        try 
        {
            // Валидируем входные данные
            if (string.IsNullOrEmpty(request.Recipient) || string.IsNullOrEmpty(request.Body))
            {
                return BadRequest("Recipient and Body are required"); // Возвращаем ошибку валидации
            }
            
            // Вызываем сервис отправки email
            await _emailSender.SendAsync(request.Recipient, request.Subject, request.Body);
            
            // Логируем успешную отправку
            _logger.LogInformation("Email sent to {Recipient}", request.Recipient);
            
            return Ok(); // Возвращаем успешный статус
        }
        catch (Exception ex) 
        {
            // Логируем ошибку и возвращаем статус 500
            _logger.LogError(ex, "Failed to send email to {Recipient}", request.Recipient);
            return StatusCode(500, "Internal server error");
        }
    }
}

// Улучшенный EmailSender с асинхронностью
public class EmailSender 
{
    private readonly SmtpClient _smtpClient; // SMTP клиент для отправки
    private readonly ILogger<EmailSender> _logger; // Логгер для сервиса

    public async Task SendAsync(string recipient, string subject, string message) 
    {
        // Создаем объект письма
        var mailMessage = new MailMessage 
        {
            From = new MailAddress("noreply@company.com"), // Адрес отправителя
            Subject = subject, // Тема письма
            Body = message, // Содержимое
            IsBodyHtml = false // Указываем, что содержимое не HTML
        };
        
        // Добавляем получателя
        mailMessage.To.Add(recipient);
        
        try 
        {
            // Асинхронно отправляем письмо через SMTP
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email successfully sent to {Recipient}", recipient);
        }
        finally 
        {
            // Освобождаем ресурсы
            mailMessage.Dispose();
        }
    }
}

// ========================================
// СПОСОБ 2: АСИНХРОННАЯ ИНТЕГРАЦИЯ ЧЕРЕЗ ОЧЕРЕДЬ СООБЩЕНИЙ
// ========================================

// Application A - UserService с публикацией в очередь
public class UserService 
{
    private readonly IValidator<UserModel> _validator; // Валидатор модели
    private readonly IUserRepository _userRepository; // Репозиторий пользователей
    private readonly IUnitOfWork _unitOfWork; // Unit of Work для транзакций
    private readonly IMessagePublisher _messagePublisher; // Издатель сообщений в очередь
    private readonly ILogger<UserService> _logger; // Логгер

    public async Task RegisterUser(UserModel user) 
    {
        // Валидируем данные пользователя
        await _validator.ValidateAndThrowAsync(user);
        
        // Сохраняем пользователя в БД
        await _userRepository.CreateAsync(user);
        
        // Фиксируем транзакцию
        await _unitOfWork.SaveChangesAsync();
        
        // Создаем событие для отправки email
        var emailEvent = new UserRegisteredEvent 
        {
            UserId = user.Id, // ID зарегистрированного пользователя
            Email = user.Email, // Email для отправки уведомления
            UserName = user.Name, // Имя пользователя для персонализации
            RegistrationDate = DateTime.UtcNow // Время регистрации
        };
        
        try 
        {
            // Публикуем событие в очередь сообщений (например, RabbitMQ/Kafka)
            await _messagePublisher.PublishAsync("user.registered", emailEvent);
            
            _logger.LogInformation("Registration event published for user {UserId}", user.Id);
        }
        catch (Exception ex) 
        {
            // Логируем ошибку публикации, но не прерываем процесс
            _logger.LogError(ex, "Failed to publish registration event for user {UserId}", user.Id);
            // В production можно добавить компенсирующие механизмы
        }
    }
}

// Событие регистрации пользователя
public class UserRegisteredEvent 
{
    public Guid UserId { get; set; } // Уникальный идентификатор пользователя
    public string Email { get; set; } // Email адрес
    public string UserName { get; set; } // Имя пользователя
    public DateTime RegistrationDate { get; set; } // Дата и время регистрации
}

// Application B - обработчик событий из очереди
public class UserRegisteredEventHandler : IMessageHandler<UserRegisteredEvent> 
{
    private readonly EmailSender _emailSender; // Сервис отправки email
    private readonly ILogger<UserRegisteredEventHandler> _logger; // Логгер обработчика

    public async Task HandleAsync(UserRegisteredEvent @event) 
    {
        try 
        {
            // Формируем персонализированное сообщение
            var subject = "Welcome to our service!";
            var body = $"Hello {@event.UserName}, thank you for registering on {DateTime.Now:yyyy-MM-dd}!";
            
            // Отправляем welcome email
            await _emailSender.SendAsync(@event.Email, subject, body);
            
            _logger.LogInformation("Welcome email sent for user {UserId}", @event.UserId);
        }
        catch (Exception ex) 
        {
            // Логируем ошибку обработки события
            _logger.LogError(ex, "Failed to send welcome email for user {UserId}", @event.UserId);
            
            // В реальном проекте здесь должна быть логика повторных попыток
            // или отправка в dead letter queue
            throw; // Перебрасываем исключение для механизма повторов
        }
    }
}

// ========================================
// СПОСОБ 3: ПАТТЕРН OUTBOX ДЛЯ НАДЕЖНОСТИ
// ========================================

// Сущность для хранения исходящих событий
public class OutboxEvent 
{
    public Guid Id { get; set; } // Уникальный идентификатор события
    public string EventType { get; set; } // Тип события
    public string Payload { get; set; } // Сериализованные данные события
    public DateTime CreatedAt { get; set; } // Время создания
    public bool IsProcessed { get; set; } // Флаг обработки
    public int RetryCount { get; set; } // Количество попыток обработки
    public DateTime? ProcessedAt { get; set; } // Время успешной обработки
}

// UserService с использованием Outbox паттерна
public class UserService 
{
    private readonly IValidator<UserModel> _validator; // Валидатор
    private readonly IUserRepository _userRepository; // Репозиторий пользователей
    private readonly IOutboxRepository _outboxRepository; // Репозиторий для Outbox
    private readonly IUnitOfWork _unitOfWork; // Unit of Work
    private readonly ILogger<UserService> _logger; // Логгер

    public async Task RegisterUser(UserModel user) 
    {
        // Валидируем входные данные
        await _validator.ValidateAndThrowAsync(user);
        
        // Начинаем транзакцию для атомарности операций
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try 
        {
            // Сохраняем пользователя в БД
            await _userRepository.CreateAsync(user);
            
            // Создаем событие для Outbox
            var emailEvent = new UserRegisteredEvent 
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.Name,
                RegistrationDate = DateTime.UtcNow
            };
            
            // Сохраняем событие в Outbox в той же транзакции
            var outboxEvent = new OutboxEvent 
            {
                Id = Guid.NewGuid(),
                EventType = nameof(UserRegisteredEvent),
                Payload = JsonSerializer.Serialize(emailEvent), // Сериализуем событие
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                RetryCount = 0
            };
            
            await _outboxRepository.CreateAsync(outboxEvent);
            
            // Фиксируем транзакцию (и пользователь, и событие сохранены атомарно)
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("User {UserId} registered and outbox event created", user.Id);
        }
        catch 
        {
            // Откатываем транзакцию при ошибке
            await transaction.RollbackAsync();
            throw; // Перебрасываем исключение
        }
    }
}

// Фоновый сервис для обработки Outbox событий
public class OutboxProcessor : BackgroundService 
{
    private readonly IServiceProvider _serviceProvider; // Провайдер сервисов для создания scope
    private readonly ILogger<OutboxProcessor> _logger; // Логгер процессора
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10); // Интервал обработки

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
    {
        // Бесконечный цикл обработки событий
        while (!stoppingToken.IsCancellationRequested) 
        {
            try 
            {
                // Создаем новый scope для каждой итерации
                using var scope = _serviceProvider.CreateScope();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
                
                // Получаем необработанные события
                var events = await outboxRepo.GetUnprocessedEventsAsync(batchSize: 10);
                
                foreach (var outboxEvent in events) 
                {
                    try 
                    {
                        // Публикуем событие в очередь сообщений
                        await messagePublisher.PublishAsync(
                            outboxEvent.EventType.ToLowerInvariant(), 
                            outboxEvent.Payload
                        );
                        
                        // Помечаем событие как обработанное
                        outboxEvent.IsProcessed = true;
                        outboxEvent.ProcessedAt = DateTime.UtcNow;
                        
                        await outboxRepo.UpdateAsync(outboxEvent);
                        
                        _logger.LogInformation("Outbox event {EventId} processed successfully", outboxEvent.Id);
                    }
                    catch (Exception ex) 
                    {
                        // Увеличиваем счетчик попыток
                        outboxEvent.RetryCount++;
                        await outboxRepo.UpdateAsync(outboxEvent);
                        
                        _logger.LogError(ex, "Failed to process outbox event {EventId}, retry count: {RetryCount}", 
                            outboxEvent.Id, outboxEvent.RetryCount);
                        
                        // Если превысили максимальное количество попыток, можно переместить в dead letter
                        if (outboxEvent.RetryCount >= 3) 
                        {
                            _logger.LogWarning("Outbox event {EventId} moved to dead letter after {RetryCount} attempts", 
                                outboxEvent.Id, outboxEvent.RetryCount);
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error in outbox processor");
            }
            
            // Ждем перед следующей итерацией
            await Task.Delay(_processingInterval, stoppingToken);
        }
    }
}

/*Сравнение способов интеграции:
1. Синхронный HTTP API вызов
Преимущества:
-Простота реализации и понимания
-Немедленная обратная связь об успехе/неудаче
-Легко тестировать и отлаживать

Недостатки:
Сильная связанность между сервисами
-Увеличение времени отклика регистрации
-Сервис A зависит от доступности сервиса B
-При падении email-сервиса может сорваться вся регистрация

2. Асинхронная интеграция через очередь сообщений
Преимущества:
Слабая связанность сервисов
-Высокая отказоустойчивость
-Возможность горизонтального масштабирования
-Механизмы повторных попыток и dead letter queue

Недостатки:
-Сложность инфраструктуры (Kafka, RabbitMQ)
-Eventual consistency
-Сложнее отлаживать распределенные процессы

3. Паттерн Outbox
Преимущества:
-Гарантированная доставка событий
-Атомарность операций (пользователь + событие в одной транзакции)
-Решает проблему "dual writes"

Недостатки:
-Сложность реализации
-Дополнительная нагрузка на БД
-Необходимость фонового процесса

Рекомендации по протоколам:
-HTTP/REST - для синхронного взаимодействия
-AMQP (RabbitMQ) - для надежной доставки сообщений
-Apache Kafka - для высоконагруженных систем с множественными подписчиками
-gRPC - для высокопроизводительного взаимодействия между сервисами

Возможные проблемы и их решения:
-Network failures - Circuit Breaker паттерн, retry policies
-Дублирование сообщений - идемпотентность операций
-Порядок обработки - partitioning в Kafka
-Мониторинг - distributed tracing, health checks
-Безопасность - аутентификация через JWT/OAuth, TLS шифрование

Мой выбор для production: Комбинация асинхронной интеграции через Kafka с Outbox паттерном для критически важных уведомлений.*/
