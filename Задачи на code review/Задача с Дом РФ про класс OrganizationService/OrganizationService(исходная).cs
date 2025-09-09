public class OrganizationService
{
    private readonly FileStorageService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IProjectExternalApi _projectExternalApi;
    private readonly AppDbContext _dbContext;

    public OrganizationService(
        FileStorageService fileService,
        INotificationService notificationService,
        IProjectExternalApi projectExternalApi,
        AppDbContext dbContext)
    {
        _fileService = fileService;
        _notificationService = notificationService;
        _projectExternalApi = projectExternalApi;
        _dbContext = dbContext;
    }

    public byte[] GetOrganizationLogo(Guid organizationId)
    {
        var organizations = _dbContext.Organizations.ToList();
        var organization = organizations.Where(entity => entity.Id == organizationId).First();

        var logoId = organization.LogoId;

        var memoryStream = new MemoryStream();
        Stream image = _fileService.GetLogoById(logoId);
        image.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    public void SetOrganizationLogo(Guid organizationId, Stream logo)
    {
        var logoId = _fileService.UploadLogo(logo);

        var organizations = _dbContext.Organizations.ToList();
        var organization = organizations.Where(entity => entity.Id == organizationId).First();

        organization.LogoId = logoId;

        _dbContext.SaveChangesAsync(default);

        _notificationService.NotifyLogoChanged(organization.Id);
    }

    public async Task<long> Calculate(Guid organizationId, DateTime? start, DateTime? finished, CancellationToken ct)
    {
        var organizations = _dbContext.Organizations.ToList();
        var organization = organizations.Where(entity => entity.Id == organizationId).First();

        var projects = await _projectExternalApi.GetProjectsAsync(organization.Id, default);

        var result = 0;

        foreach (var project in projects)
        {
            if (project.CompleteDate > start || project.CompleteDate < finished)
            {
                result = result + project.Cost;
            }
        }

        return result;
    }
}
