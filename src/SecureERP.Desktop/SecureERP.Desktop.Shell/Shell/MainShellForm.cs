using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Shell.Shell;

public sealed class MainShellForm : RibbonForm
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<IDesktopModule> _modules;
    private readonly IPermissionService _permissionService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ISessionContext _sessionContext;

    private readonly RibbonControl _ribbon;
    private readonly AccordionControl _navigation;
    private readonly PanelControl _workspace;
    private readonly DocumentManager _documentManager;
    private readonly TabbedView _tabbedView;
    private readonly DockManager _dockManager;
    private readonly BarStaticItem _userStatusItem;

    public MainShellForm(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<IDesktopModule> modules,
        IPermissionService permissionService,
        IAuthenticationService authenticationService,
        ISessionContext sessionContext)
    {
        _serviceProvider = serviceProvider;
        _modules = modules;
        _permissionService = permissionService;
        _authenticationService = authenticationService;
        _sessionContext = sessionContext;

        Text = "SecureERP Desktop";
        WindowState = FormWindowState.Maximized;

        _ribbon = BuildRibbon();
        _navigation = BuildNavigation();
        _workspace = BuildWorkspace();
        _tabbedView = new TabbedView();
        _documentManager = BuildDocumentManager(_tabbedView);
        _dockManager = BuildDockManager();

        _userStatusItem = new BarStaticItem
        {
            Caption = $"Usuario: {_sessionContext.Current?.UserName ?? "N/A"}"
        };
        _ribbon.Items.Add(_userStatusItem);

        var statusBar = new RibbonStatusBar
        {
            Dock = DockStyle.Bottom,
            Ribbon = _ribbon
        };
        statusBar.ItemLinks.Add(_userStatusItem);

        Controls.Add(_workspace);
        Controls.Add(_navigation);
        Controls.Add(statusBar);
        Controls.Add(_ribbon);

        Ribbon = _ribbon;
        StatusBar = statusBar;

        Load += (_, _) =>
        {
            BuildNavigationTree();
            OpenDefaultDocument();
        };
    }

    private RibbonControl BuildRibbon()
    {
        var ribbon = new RibbonControl
        {
            Dock = DockStyle.Top,
            ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False,
            ToolbarLocation = RibbonQuickAccessToolbarLocation.Hidden
        };

        var homePage = new RibbonPage("Inicio");
        var sessionGroup = new RibbonPageGroup("Sesion");
        var actionsGroup = new RibbonPageGroup("Acciones");

        var refreshButton = new BarButtonItem
        {
            Caption = "Refrescar"
        };
        refreshButton.ItemClick += (_, _) => RefreshCurrentDocument();

        var signOutButton = new BarButtonItem
        {
            Caption = "Cerrar sesion"
        };
        signOutButton.ItemClick += async (_, _) => await SignOutAsync();

        sessionGroup.ItemLinks.Add(signOutButton);
        actionsGroup.ItemLinks.Add(refreshButton);
        homePage.Groups.Add(sessionGroup);
        homePage.Groups.Add(actionsGroup);

        ribbon.Pages.Add(homePage);
        return ribbon;
    }

    private AccordionControl BuildNavigation()
    {
        var navigation = new AccordionControl
        {
            Dock = DockStyle.Left,
            Width = 280,
            ShowFilterControl = DevExpress.XtraBars.Navigation.ShowFilterControl.Always,
            ViewType = AccordionControlViewType.HamburgerMenu
        };
        navigation.ElementClick += (_, args) =>
        {
            if (args.Element.Style == ElementStyle.Item && args.Element.Tag is NavigationItemDefinition item)
            {
                OpenDocument(item);
            }
        };

        return navigation;
    }

    private static PanelControl BuildWorkspace()
    {
        return new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };
    }

    private DocumentManager BuildDocumentManager(TabbedView tabbedView)
    {
        var documentManager = new DocumentManager
        {
            ContainerControl = this,
            View = tabbedView
        };
        documentManager.ViewCollection.Add(tabbedView);

        return documentManager;
    }

    private DockManager BuildDockManager()
    {
        var dockManager = new DockManager(this);
        var inspectorPanel = dockManager.AddPanel(DockingStyle.Right);
        inspectorPanel.Text = "Inspector";
        inspectorPanel.Visibility = DockVisibility.AutoHide;
        inspectorPanel.OriginalSize = new Size(280, 240);

        var content = new LabelControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Text = "Panel reservado para filtros avanzados, actividad y auditoria resumida."
        };
        inspectorPanel.Controls.Add(content);

        return dockManager;
    }

    private void BuildNavigationTree()
    {
        _navigation.Elements.Clear();

        foreach (var module in _modules.OrderBy(m => m.ModuleCaption))
        {
            var group = new AccordionControlElement
            {
                Text = module.ModuleCaption,
                Style = ElementStyle.Group,
                Expanded = true
            };

            foreach (var item in module.GetNavigationItems())
            {
                if (!_permissionService.CanAccess(item.PermissionKey))
                {
                    continue;
                }

                var child = new AccordionControlElement
                {
                    Text = item.Caption,
                    Style = ElementStyle.Item,
                    Tag = item
                };
                group.Elements.Add(child);
            }

            if (group.Elements.Count > 0)
            {
                _navigation.Elements.Add(group);
            }
        }
    }

    private void OpenDefaultDocument()
    {
        var defaultItem = _modules
            .SelectMany(module => module.GetNavigationItems())
            .FirstOrDefault(item => item.ItemKey == "home.dashboard" && _permissionService.CanAccess(item.PermissionKey))
            ?? _modules.SelectMany(module => module.GetNavigationItems())
                .FirstOrDefault(item => _permissionService.CanAccess(item.PermissionKey));

        if (defaultItem is not null)
        {
            OpenDocument(defaultItem);
        }
    }

    private void OpenDocument(NavigationItemDefinition item)
    {
        if (item.Singleton)
        {
            var existing = _tabbedView.Documents.FirstOrDefault(d => Equals(d.Control?.Tag, item.ItemKey));
            if (existing is not null)
            {
                _tabbedView.Controller.Activate(existing);
                return;
            }
        }

        if (ActivatorUtilities.CreateInstance(_serviceProvider, item.ViewType) is not Control control)
        {
            throw new InvalidOperationException($"No se pudo crear la vista {item.ViewType.FullName}.");
        }

        control.Tag = item.ItemKey;
        control.Dock = DockStyle.Fill;

        var document = _tabbedView.AddDocument(control);
        document.Caption = item.Caption;
        _tabbedView.Controller.Activate(document);
    }

    private void RefreshCurrentDocument()
    {
        if (ActiveControl is null)
        {
            return;
        }

        ActiveControl.Refresh();
    }

    private async Task SignOutAsync()
    {
        await _authenticationService.SignOutAsync();
        Close();
    }
}
