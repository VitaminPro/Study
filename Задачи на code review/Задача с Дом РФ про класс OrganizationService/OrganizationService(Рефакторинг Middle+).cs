// ===============================================================================
// УЛУЧШЕННАЯ ВЕРСИЯ С ОБЪЯСНЕНИЯМИ АРХИТЕКТУРНЫХ РЕШЕНИЙ
// ===============================================================================

// ✅ АРХИТЕКТУРНЫЙ ПАТТЕРН: Application Service (Clean Architecture)
// ПОЧЕМУ: Координирует выполнение бизнес use cases, не содержит бизнес-логики
// ПРИНЦИП: Тонкий слой между UI и Domain/Infrastructure
public class OrganizationService
{
    // ✅ DEPENDENCY INVERSION PRINCIPLE: Все зависимости через интерфейсы
    // ПОЧЕМУ: Упрощает тестирование и позволяет менять реализации без изменения кода
    private readonly FileStorageService _fileService;              // ⚠️ ДОЛЖНО БЫТЬ: IFileStorageService
    private readonly INotificationService _notificationService;    // ✅ Правильно
    private readonly IProjectExternalApi _projectExternaApi;       // ✅ Правильно  
    private readonly AppDbContext _dbContext;                      // ⚠️ ДОЛЖНО БЫТЬ: IOrganizationRepository
    private readonly ILogger<OrganizationService> _logger;         // ✅ Структурированное логирование

    // ✅ CONSTRUCTOR INJECTION: Единственный способ создать валидный объект
    // ПРИНЦИП: Fail Fast - если зависимости не переданы, объект не создается
    public OrganizationService(
        FileStorageService fileService,           // ⚠️ ИСПРАВИТЬ: IFileStorageService
        INotificationService notificationService,
        IProjectExternalApi projectExternalApi,
        AppDbContext dbContext,                   // ⚠️ ИСПРАВИТЬ: IOrganizationRepository
        ILogger<OrganizationService> logger)
    {
        // ✅ GUARD CLAUSES: Ранняя валидация параметров
        // ПРИНЦИП: Fail Fast - обнаруживаем ошибки как можно раньше
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _projectExternalApi = projectExternalApi ?? throw new ArgumentNullException(nameof(projectExternalApi));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ✅ USE CASE: Получение логотипа организации
    // ASYNC PATTERN: I/O операции всегда асинхронные для лучшей производительности
    public async Task<byte[]> GetOrganizationLogoAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // ✅ INPUT VALIDATION: Проверяем корректность входных данных
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));

        // ✅ EFFICIENT QUERY: Только нужная запись + AsNoTracking для Read-Only операций
        // ПРОИЗВОДИТЕЛЬНОСТЬ: AsNoTracking() отключает Change Tracking для лучшей производительности
        var organization = await _dbContext.Organizations
            .AsNoTracking()  // Не отслеживаем изменения - это только чтение
            .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

        // ✅ DEFENSIVE PROGRAMMING: Проверяем существование объекта
        if (organization == null)
            throw new OrganizationNotFoundException($"Organization with ID {organizationId} not found");

        // ✅ BUSINESS RULE: Логотип может отсутствовать - возвращаем пустой массив
        if (organization.LogoId == null)
            return Array.Empty<byte>();

        try
        {
            // ✅ RESOURCE MANAGEMENT: using для автоматического освобождения ресурсов
            // ПРИНЦИП: RAII (Resource Acquisition Is Initialization)
            using var logoStream = _fileService.GetLogoById(organization.LogoId.Value);
            using var memoryStream = new MemoryStream();
            
            // ✅ ASYNC I/O: Не блокируем поток при копировании данных
            await logoStream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            // ✅ STRUCTURED LOGGING: Контекстная информация для диагностики
            _logger.LogError(ex, "Failed to retrieve logo for organization {OrganizationId}", organizationId);
            throw new LogoRetrievalException("Failed to retrieve organization logo", ex);
        }
    }

    // ✅ USE CASE: Установка логотипа организации
    public async Task SetOrganizationLogoAsync(Guid organizationId, Stream logo, CancellationToken cancellationToken = default)
    {
        // ✅ COMPREHENSIVE INPUT VALIDATION
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        
        if (logo == null)
            throw new ArgumentNullException(nameof(logo));

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

        if (organization == null)
            throw new OrganizationNotFoundException($"Organization with ID {organizationId} not found");

        try
        {
            // ✅ BUSINESS LOGIC: Сначала загружаем новый логотип
            var logoId = await _fileService.UploadLogoAsync(logo, cancellationToken);
            
            // ✅ CLEANUP: Удаляем старый логотип чтобы избежать утечки storage
            // ПРИНЦИП: Leave No Trace - очищаем неиспользуемые ресурсы
            if (organization.LogoId.HasValue)
            {
                await _fileService.DeleteLogoAsync(organization.LogoId.Value, cancellationToken);
            }

            organization.LogoId = logoId;
            
            // ✅ PROPER ASYNC: Используем await для гарантии сохранения
            // КРИТИЧНО: Без await данные могут быть потеряны
            await _dbContext.SaveChangesAsync(cancellationToken);

            // ✅ ORDERING: Уведомление ТОЛЬКО после успешного сохранения в БД
            // ПРИНЦИП: Eventual Consistency - сначала данные, потом уведомления
            await _notificationService.NotifyLogoChangedAsync(organization.Id, cancellationToken);
            
            // ✅ OBSERVABILITY: Логируем успешные операции для мониторинга
            _logger.LogInformation("Logo updated for organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set logo for organization {OrganizationId}", organizationId);
            throw new LogoUpdateException("Failed to update organization logo", ex);
        }
    }

    // ✅ USE CASE: Расчет стоимости проектов с фильтрацией по датам
    public async Task<long> CalculateProjectCostsAsync(
        Guid organizationId, 
        DateTime? startDate,    // ✅ OPTIONAL FILTER: null означает "без ограничения"
        DateTime? endDate,      // ✅ OPTIONAL FILTER: null означает "без ограничения" 
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));

        var organization = await _dbContext.Organizations
            .AsNoTracking()  // Read-only операция
            .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

        if (organization == null)
            throw new OrganizationNotFoundException($"Organization with ID {organizationId} not found");

        try
        {
            // ✅ ПРАВИЛЬНОЕ ИСПОЛЬЗОВАНИЕ CancellationToken: передаем полученный токен
            // ПРИНЦИП: Cooperative Cancellation - позволяем отменить долгую операцию
            var projects = await _projectExternalApi.GetProjectsAsync(organization.Id, cancellationToken);

            // ✅ ИСПРАВЛЕННАЯ БИЗНЕС-ЛОГИКА: правильная фильтрация по диапазону дат
            // БЫЛО: project.CompleteDate > start || project.CompleteDate < finished (НЕВЕРНО)
            // СТАЛО: project.CompleteDate >= start && project.CompleteDate <= finished (ВЕРНО)
            var filteredProjects = projects.Where(project => 
                IsProjectInDateRange(project.CompleteDate, startDate, endDate));

            // ✅ FUNCTIONAL APPROACH: используем LINQ для агрегации
            var totalCost = filteredProjects.Sum(project => project.Cost);
            
            // ✅ DEBUGGING SUPPORT: логируем результат расчета для отладки
            _logger.LogDebug("Calculated total cost {TotalCost} for organization {OrganizationId} " +
                           "with date range {StartDate} - {EndDate}", 
                           totalCost, organizationId, startDate, endDate);

            return totalCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate costs for organization {OrganizationId}", organizationId);
            throw new CostCalculationException("Failed to calculate project costs", ex);
        }
    }

    // ✅ PURE FUNCTION: Статический метод без побочных эффектов
    // ПРИНЦИПЫ: Functional Programming - детерминированность, легкость тестирования
    // ПОЧЕМУ STATIC: Не зависит от состояния объекта, можно unit-тестировать изолированно
    private static bool IsProjectInDateRange(DateTime? completeDate, DateTime? startDate, DateTime? endDate)
    {
        // ✅ EDGE CASE: проекты без даты завершения исключаем
        if (!completeDate.HasValue)
            return false;

        var date = completeDate.Value;
        
        // ✅ NULL-SAFE: если границы не заданы, считаем что ограничения нет
        var afterStart = !startDate.HasValue || date >= startDate.Value;   // null startDate = "с самого начала"
        var beforeEnd = !endDate.HasValue || date <= endDate.Value;        // null endDate = "до конца времен"
        
        // ✅ AND LOGIC: проект должен быть И после начала, И до конца
        return afterStart && beforeEnd;
    }
}

// ✅ DOMAIN EXCEPTIONS: Специфичные исключения для разных типов ошибок
// ПРИНЦИП: Explicit Error Handling - ошибки явно типизированы
public class OrganizationNotFoundException : Exception
{
    public OrganizationNotFoundException(string message) : base(message) { }
}

public class LogoRetrievalException : Exception
{
    public LogoRetrievalException(string message, Exception innerException) : base(message, innerException) { }
}

public class LogoUpdateException : Exception
{
    public LogoUpdateException(string message, Exception innerException) : base(message, innerException) { }
}

public class CostCalculationException : Exception
{
    public CostCalculationException(string message, Exception innerException) : base(message, innerException) { }
}

/*
ПОВТОРНАЯ ОЦЕНКА УЛУЧШЕННОГО КОДА:
1. КОРРЕКТНОСТЬ: 8/10 ⬆️ (+6) - все критические ошибки исправлены
2. АРХИТЕКТУРА: 8/10 ⬆️ (+5) - следует SOLID принципам
3. ЧИТАЕМОСТЬ: 8/10 ⬆️ (+5) - хорошие практики C#
4. БЕЗОПАСНОСТЬ: 8/10 ⬆️ (+7) - нет утечек памяти и race conditions
5. ПРОИЗВОДИТЕЛЬНОСТЬ: 8/10 ⬆️ (+7) - эффективные запросы
УРОВЕНЬ КАНДИДАТА: Middle/Middle+ ⬆️ (было Junior-)
ИСПРАВЛЕННЫЕ ПРОБЛЕМЫ:
✅ Проблема N+1 запросов → Эффективные запросы с FirstOrDefaultAsync
✅ Асинхронность по принципу "запустил-и-забудь" (Fire-and-forget) → Корректное использование await
✅ Утечки ресурсов → Использование блоков using
✅ Логические ошибки → Исправленная фильтрация дат
✅ Слабая обработка ошибок → Структурированные исключения и логирование
✅ Отсутствие валидации → Комплексная проверка входных данных
АРХИТЕКТУРНЫЕ УЛУЧШЕНИЯ:
✅ Повсеместное использование шаблонов async/await
✅ Поддержка CancellationToken
✅ Структурированное логирование
✅ Оборонительное программирование (Defensive programming)
✅ Чистые функции (Pure functions) где это возможно
✅ Управление ресурсами
✅ Специализированные исключения предметной области (Domain-specific exceptions)
Пояснения ключевых терминов:
1)"Запустил-и-забудь" (Fire-and-forget): Термин описывает опасную практику вызова асинхронного метода без await.
2)Оборонительное программирование: Подход, предполагающий проверку входных данных и состояний для предотвращения сбоев.
3)Чистые функции: Функции без побочных эффектов, результат которых зависит только от входных аргументов.
4)Специализированные исключения предметной области: Кастомные классы исключений, отражающие ошибки конкретной бизнес-логики.
РЕКОМЕНДАЦИИ ДЛЯ PRODUCTION:
1. Добавить IOrganizationRepository вместо прямого DbContext
2. Добавить IFileStorageService интерфейс
3. Рассмотреть Result<T> pattern вместо exceptions
4. Добавить retry policies для внешних вызовов
5. Добавить кэширование для часто запрашиваемых данных
6. Добавить метрики и health checks
*/
