using BioMedDocManager.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BioMedDocManager.Helpers;

public sealed class DbStringLocalizer : IStringLocalizer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DbStringLocalizer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private string GetValue(string key)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbLoc = scope.ServiceProvider.GetRequiredService<IDbLocalizer>();
        return dbLoc.T(key) ?? key;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetValue(name);
            return new LocalizedString(name, value, resourceNotFound: value == name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var fmt = GetValue(name);
            var value = string.Format(fmt, arguments);
            return new LocalizedString(name, value, resourceNotFound: fmt == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => Enumerable.Empty<LocalizedString>();

    public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
}
