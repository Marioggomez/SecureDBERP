using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using SecureERP.WinForms.Services.Navigation;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms.Shell;

public sealed class MainShellForm : RibbonForm
{
    private readonly IReadOnlyList<NavigationItemDefinition> _navigationItems;
    private readonly RibbonControl _ribbon;
    private readonly AccordionControl _accordion;
    private readonly DocumentManager _documentManager;
    private readonly TabbedView _tabbedView;

    public MainShellForm(
        IReadOnlyList<NavigationItemDefinition> navigationItems,
        IThemePreferenceService themePreferenceService)
    {
        _navigationItems = navigationItems;

        Text = "SecureERP - Desktop";
        WindowState = FormWindowState.Maximized;
        IsMdiContainer = true;

        ThemePreference current = themePreferenceService.Load();
        UserLookAndFeel.Default.SetSkinStyle(current.SkinName);

        _ribbon = BuildRibbon();
        _accordion = BuildNavigation();

        _tabbedView = new TabbedView();
        _documentManager = new DocumentManager
        {
            MdiParent = this,
            View = _tabbedView,
            MenuManager = _ribbon
        };

        Controls.Add(_accordion);
        Controls.Add(_ribbon);
        Ribbon = _ribbon;

        Shown += (_, _) => OpenStartupItems();
    }

    private RibbonControl BuildRibbon()
    {
        RibbonControl ribbon = new()
        {
            Dock = DockStyle.Top,
            ApplicationButtonText = "SecureERP"
        };

        RibbonPage homePage = new("Inicio");
        RibbonPageGroup modulesGroup = new("Módulos");
        RibbonPageGroup appearanceGroup = new("Apariencia");

        BarButtonItem homeButton = CreateOpenModuleButton(ribbon, "HOME.DASHBOARD", "Inicio");
        BarButtonItem searchButton = CreateOpenModuleButton(ribbon, "SEARCH.CATALOG", "Búsqueda Global");
        BarButtonItem appearanceButton = CreateOpenModuleButton(ribbon, "SYSTEM.APPEARANCE", "Skins");

        SkinRibbonGalleryBarItem skinGallery = new();
        ribbon.Items.Add(skinGallery);

        modulesGroup.ItemLinks.Add(homeButton);
        modulesGroup.ItemLinks.Add(searchButton);
        modulesGroup.ItemLinks.Add(appearanceButton);
        appearanceGroup.ItemLinks.Add(skinGallery);

        homePage.Groups.Add(modulesGroup);
        homePage.Groups.Add(appearanceGroup);

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

        XtraForm? existing = MdiChildren
            .OfType<XtraForm>()
            .FirstOrDefault(form => string.Equals(form.Tag?.ToString(), key, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        XtraForm view = definition.CreateView();
        view.MdiParent = this;
        view.Tag = key;
        view.Text = definition.Caption;
        view.Show();
    }
}

