// Интерфейсы — чтобы можно было мокать/тестировать
public interface IFileStorageService
{
    // Возвращает поток с картинкой (вызов должен вернуть открытый Stream, который вызывающий обязан закрыть)
    Task<Stream?> GetLogoByIdAsync(Guid logoId, CancellationToken cancellationToken);
    // Загружает логотип и возвращает идентификатор ресурса
    Task<Guid> UploadLogoAsync(Stream content, CancellationToken cancellationToken);
}

public interface INotificationService
{
    Task NotifyLogoChangedAsync(Guid organizationId, CancellationToken cancellationToken);
}

public interface IProjectExternalApi
{
    // Возвращает коллекцию проектов (можно обсудить стриминг/фильтрацию на стороне API)
    Task<IReadOnlyCollection<ProjectDto>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken);
}

// DTO проекта из внешнего API
public class ProjectDto
{
    public DateTime CompleteDate { get; set; }
    public long Cost { get; set; } // для денег лучше decimal, но оставляю long по исходнику (обсудим на собесе)
}

// Сервис
public class OrganizationService
{
    private readonly IFileStorageService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IProjectExternalApi _projectExternalApi;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IFileStorageService fileService,
        INotificationService notificationService,
        IProjectExternalApi projectExternalApi,
        AppDbContext dbContext,
        ILogger<OrganizationService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _projectExternalApi = projectExternalApi ?? throw new ArgumentNullException(nameof(projectExternalApi));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Возвращает байты логотипа организации.
    /// Асинхронно, поддерживает отмену.
    /// </summary>
    public async Task<byte[]?> GetOrganizationLogoAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        // Не загружаем всю таблицу — делаем целевой запрос
        var organization = await _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization == null)
        {
            _logger.LogWarning("Organization {OrgId} not found when getting logo", organizationId);
            return null; // или throw new EntityNotFoundException(...)
        }

        if (organization.LogoId == Guid.Empty)
        {
            _logger.LogInformation("Organization {OrgId} has no logo", organizationId);
            return null;
        }

        // Получаем поток от хранилища и копируем асинхронно в память
        var stream = await _fileService.GetLogoByIdAsync(organization.LogoId, cancellationToken);
        if (stream == null)
        {
            _logger.LogWarning("Logo {LogoId} for org {OrgId} not found in file storage", organization.LogoId, organizationId);
            return null;
        }

        await using (stream) // гарантируем Dispose
        await using var memory = new MemoryStream();
        {
            await stream.CopyToAsync(memory, cancellationToken);
            return memory.ToArray();
        }
    }

    /// <summary>
    /// Загрузить новый логотип и привязать к организации.
    /// Сохраняет изменения в БД и уведомляет внешнюю систему.
    /// </summary>
    public async Task SetOrganizationLogoAsync(Guid organizationId, Stream logoStream, CancellationToken cancellationToken)
    {
        if (logoStream == null) throw new ArgumentNullException(nameof(logoStream));

        // Загружаем в файловое хранилище (асинхронно)
        Guid newLogoId = await _fileService.UploadLogoAsync(logoStream, cancellationToken);

        // Находим организацию целевым запросом
        var organization = await _dbContext.Organizations
            .SingleOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization == null)
        {
            _logger.LogWarning("Organization {OrgId} not found when setting logo", organizationId);
            // Возможно нужно удалить загруженный файл (в зависимости от политики) — обсудим
            throw new InvalidOperationException("Organization not found");
        }

        // Меняем и сохраняем
        organization.LogoId = newLogoId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Уведомляем подписчиков (await — чтобы вызов гарантированно выполнен)
        await _notificationService.NotifyLogoChangedAsync(organization.Id, cancellationToken);

        _logger.LogInformation("Logo for organization {OrgId} updated (LogoId: {LogoId})", organization.Id, newLogoId);
    }

    /// <summary>
    /// Считает сумму стоимостей завершённых проектов в интервале [start, finished].
    /// Если start == null — берём DateTime.MinValue, если finished == null — DateTime.MaxValue.
    /// </summary>
    public async Task<long> CalculateAsync(Guid organizationId, DateTime? start, DateTime? finished, CancellationToken cancellationToken)
    {
        // Находим организацию — чтобы убедиться, что организация существует
        var org = await _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (org == null)
        {
            _logger.LogWarning("Organization {OrgId} not found when calculating projects", organizationId);
            throw new InvalidOperationException("Organization not found");
        }

        // Получаем проекты внешнего API (передаём токен отмены)
        var projects = await _projectExternalApi.GetProjectsAsync(org.Id, cancellationToken);

        // Нормализуем границы
        var from = start ?? DateTime.MinValue;
        var to = finished ?? DateTime.MaxValue;

        // Фильтруем корректно: CompleteDate в интервале [from, to]
        var sum = projects
            .Where(p => p.CompleteDate >= from && p.CompleteDate <= to)
            .Sum(p => p.Cost);

        return sum;
    }
}
