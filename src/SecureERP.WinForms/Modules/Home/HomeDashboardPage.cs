using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using SecureERP.WinForms.Services.Workspace;

namespace SecureERP.WinForms.Modules.Home;

public sealed class HomeDashboardPage : XtraUserControl, IWorkspacePage
{
    public string PageKey => "HOME.DASHBOARD";
    public string Title => "Inicio";
    public Control MainControl => this;
    public Control? AuxiliaryPanel => null;
    public string? EntityType => null;
    public long? EntityId => null;
    public bool HasSelection => false;
    public bool IsBusy => false;
    public string? BusyMessage => null;

    public event EventHandler? ContextChanged { add { } remove { } }
    public event EventHandler? LoadingStateChanged { add { } remove { } }

    public HomeDashboardPage()
    {
        Dock = DockStyle.Fill;

        PanelControl root = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(24)
        };

        LabelControl title = new()
        {
            Text = "SecureERP Desktop",
            Appearance = { Font = new Font("Segoe UI", 24, FontStyle.Bold) },
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 56
        };

        LabelControl subtitle = new()
        {
            Text = "Framework base WinForms + DevExpress para módulos ERP empresariales.",
            Appearance = { Font = new Font("Segoe UI", 11, FontStyle.Regular) },
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 34
        };

        GroupControl quickStart = new()
        {
            Text = "Acciones iniciales",
            Dock = DockStyle.Top,
            Height = 220
        };

        MemoEdit notes = new()
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                ReadOnly = true
            },
            Text = "1) Usa la navegación para abrir Búsqueda Global.\r\n" +
                   "2) Cambia skins desde Apariencia.\r\n" +
                   "3) Extiende páginas implementando IWorkspacePage.\r\n" +
                   "4) Reutiliza RelatedInfoSidebarControl donde aplique."
        };

        quickStart.Controls.Add(notes);

        root.Controls.Add(quickStart);
        root.Controls.Add(subtitle);
        root.Controls.Add(title);

        Controls.Add(root);
    }

    public void BuildRibbon(RibbonPage page)
    {
        page.Groups.Clear();
        RibbonPageGroup shortcuts = new("Atajos");
        BarButtonItem openSearch = new(page.Ribbon.Manager, "Búsqueda Global");
        openSearch.ItemClick += (_, _) => MessageBox.Show("Abre la búsqueda desde navegación.");
        shortcuts.ItemLinks.Add(openSearch);
        page.Groups.Add(shortcuts);
    }

    public void OnActivated() { }
}
