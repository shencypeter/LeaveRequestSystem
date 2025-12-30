using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BioMedDocManager.Helpers;

public sealed class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbStringLocalizerFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IStringLocalizer Create(Type resourceSource)
        => new DbStringLocalizer(_scopeFactory);

    public IStringLocalizer Create(string baseName, string location)
        => new DbStringLocalizer(_scopeFactory);
}
