using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Core.Abstractions;

public interface IDesktopModule
{
    string ModuleKey { get; }

    string ModuleCaption { get; }

    IEnumerable<NavigationItemDefinition> GetNavigationItems();

    void Register(IServiceCollection services);
}