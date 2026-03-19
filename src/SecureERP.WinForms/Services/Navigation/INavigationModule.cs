namespace SecureERP.WinForms.Services.Navigation;

public interface INavigationModule
{
    IReadOnlyList<NavigationItemDefinition> BuildItems();
}

