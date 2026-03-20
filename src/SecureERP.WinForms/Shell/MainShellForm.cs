using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using SecureERP.WinForms.Common;
using SecureERP.WinForms.Services.Navigation;
using SecureERP.WinForms.Services.Workspace;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms.Shell;

public sealed class MainShellForm : RibbonForm
{
    private readonly IReadOnlyList<NavigationItemDefinition> _navigationItems;
    private readonly RibbonControl _ribbon;
    private readonly AccordionControl _accordion;
    private readonly DocumentManager _documentManager;
    private readonly TabbedView _tabbedView;
    private readonly RibbonPage _contextPage;
    private readonly string _shellTitle;

    public MainShellForm(
        IReadOnlyList<NavigationItemDefinition> navigationItems,
        IThemePreferenceService themePreferenceService)
    {
        _navigationItems = navigationItems;

        _shellTitle = AppBranding.ShellTitle;
        Text = _shellTitle;
        WindowState = FormWindowState.Maximized;
        IsMdiContainer = true;
        AllowFormSkin = true;
        FormBorderEffect = DevExpress.XtraEditors.FormBorderEffect.Shadow;

        ThemePreference current = themePreferenceService.Load();
        ApplyTheme(current);

        _ribbon = BuildRibbon();
        _accordion = BuildNavigation();

        _tabbedView = new TabbedView();
        _documentManager = new DocumentManager
        {
            MdiParent = this,
            View = _tabbedView,
            MenuManager = _ribbon
        };

        _contextPage = new RibbonPage("Acciones");
        _ribbon.Pages.Add(_contextPage);

        Controls.Add(_accordion);
        Controls.Add(_ribbon);
        Ribbon = _ribbon;

        _tabbedView.DocumentActivated += (_, args) => OnDocumentActivated(args.Document);
        Shown += (_, _) => OpenStartupItems();
    }

    private static void ApplyTheme(ThemePreference current)
    {
        if (!string.IsNullOrWhiteSpace(current.PaletteName))
        {
            UserLookAndFeel.Default.SetSkinStyle(current.SkinName, current.PaletteName);
        }
        else
        {
            UserLookAndFeel.Default.SetSkinStyle(current.SkinName);
        }

        UserLookAndFeel.Default.CompactUIMode = current.CompactUIMode
            ? DevExpress.Utils.DefaultBoolean.True
            : DevExpress.Utils.DefaultBoolean.False;

        WindowsFormsSettings.AllowRoundedWindowCorners = current.RoundedWindowCorners
            ? DevExpress.Utils.DefaultBoolean.True
            : DevExpress.Utils.DefaultBoolean.False;

        if (!string.IsNullOrWhiteSpace(current.AccentColorHex))
        {
            System.Drawing.Color accent = System.Drawing.ColorTranslator.FromHtml(current.AccentColorHex);
            UserLookAndFeel.Default.SkinMaskColor = accent;
            UserLookAndFeel.Default.SkinMaskColor2 = accent;
        }
    }

    private RibbonControl BuildRibbon()
    {
        RibbonControl ribbon = new()
        {
            Dock = DockStyle.Top,
            ApplicationButtonText = AppBranding.ApplicationName
        };

        RibbonPage homePage = new("Inicio");
        RibbonPageGroup modulesGroup = new("Módulos");

        BarButtonItem homeButton = CreateOpenModuleButton(ribbon, "HOME.DASHBOARD", "Inicio");
        BarButtonItem searchButton = CreateOpenModuleButton(ribbon, "SEARCH.CATALOG", "Búsqueda Global");
        BarButtonItem appearanceButton = CreateOpenModuleButton(ribbon, "SYSTEM.APPEARANCE", "Apariencia");

        modulesGroup.ItemLinks.Add(homeButton);
        modulesGroup.ItemLinks.Add(searchButton);
        modulesGroup.ItemLinks.Add(appearanceButton);

        homePage.Groups.Add(modulesGroup);
        ribbon.Pages.Add(homePage);

        return ribbon;
    }

    private AccordionControl BuildNavigation()
    {
        AccordionControl navigation = new()
        {
            Dock = DockStyle.Left,
            Width = 280,
            ViewType = AccordionControlViewType.HamburgerMenu
        };

        foreach (IGrouping<string, NavigationItemDefinition> group in _navigationItems.GroupBy(item => item.Group, StringComparer.OrdinalIgnoreCase))
        {
            AccordionControlElement groupElement = new()
            {
                Text = group.Key,
                Expanded = true,
                Style = ElementStyle.Group
            };

            foreach (NavigationItemDefinition item in group)
            {
                AccordionControlElement itemElement = new()
                {
                    Text = item.Caption,
                    Name = item.Key,
                    Style = ElementStyle.Item,
                    Tag = item.Key
                };

                itemElement.Click += (_, _) => OpenModule(item.Key);
                groupElement.Elements.Add(itemElement);
            }

            navigation.Elements.Add(groupElement);
        }

        return navigation;
    }

    private BarButtonItem CreateOpenModuleButton(RibbonControl ribbon, string key, string caption)
    {
        BarButtonItem item = new()
        {
            Caption = caption
        };
        item.ItemClick += (_, _) => OpenModule(key);
        ribbon.Items.Add(item);
        return item;
    }

    private void OpenStartupItems()
    {
        foreach (NavigationItemDefinition startupItem in _navigationItems.Where(item => item.OpenOnStartup))
        {
            OpenModule(startupItem.Key);
        }
    }

    private void OpenModule(string key)
    {
        NavigationItemDefinition? definition = _navigationItems.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
        if (definition is null)
        {
            return;
        }

        WorkspaceHostForm? existing = MdiChildren
            .OfType<WorkspaceHostForm>()
            .FirstOrDefault(form => string.Equals(form.Tag?.ToString(), key, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.Activate();
            OnWorkspaceActivated(existing.Page);
            return;
        }

        if (definition.CreateView() is not WorkspaceHostForm host)
        {
            return;
        }

        host.MdiParent = this;
        host.Tag = key;
        host.Text = definition.Caption;
        host.Show();
        OnWorkspaceActivated(host.Page);
    }

    private void OnDocumentActivated(DevExpress.XtraBars.Docking2010.Views.BaseDocument? document)
    {
        if (document?.Form is WorkspaceHostForm host)
        {
            OnWorkspaceActivated(host.Page);
        }
    }

    private void OnWorkspaceActivated(IWorkspacePage page)
    {
        _contextPage.Groups.Clear();
        // No renombrar tabs del ribbon por página (evita "Apariencia" como tab).
        // El título del formulario sí refleja la página activa.
        Text = $"{page.Title} - {_shellTitle}";
        page.BuildRibbon(_contextPage);
        page.OnActivated();
        Ribbon.SelectedPage = _contextPage;
    }
}
