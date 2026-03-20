using System.ComponentModel;
using System.Drawing;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;

namespace SecureERP.WinForms.Search;

/// <summary>
/// Contenedor reusable para resultados, grid y loading visual de una búsqueda.
/// </summary>
public sealed class SearchResultsWorkspaceControl<TItem> : XtraUserControl where TItem : class
{
    private readonly BindingList<TItem> _rows;
    private readonly GridControl _grid;
    private readonly GridView _view;
    private IOverlaySplashScreenHandle? _overlay;

    public event EventHandler? SelectionChanged;

    public GridView View => _view;
    public Control WorkspaceSurface => this;

    public SearchResultsWorkspaceControl()
    {
        Dock = DockStyle.Fill;
        _rows = new BindingList<TItem>();

        _grid = new GridControl
        {
            Dock = DockStyle.Fill,
            DataSource = _rows
        };

        _view = new GridView(_grid)
        {
            OptionsBehavior = { Editable = false, ReadOnly = true },
            OptionsView = { ShowAutoFilterRow = false, ShowGroupPanel = false },
            OptionsSelection = { MultiSelect = false, EnableAppearanceFocusedRow = true }
        };

        // Gobierno tipo NAV: orden lo pone Ribbon, no el header.
        _view.OptionsCustomization.AllowSort = false;
        _view.OptionsCustomization.AllowFilter = true;

        // Por defecto ocultamos el Find Panel: el shell/ribbon gobierna la búsqueda.
        _view.OptionsFind.AlwaysVisible = false;
        _view.OptionsFind.ShowClearButton = true;
        _view.OptionsFind.ShowFindButton = false;
        _view.FindPanelVisible = false;
        _grid.MainView = _view;

        PanelControl host = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };
        host.Controls.Add(_grid);

        Controls.Add(host);

        _view.FocusedRowChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ConfigureGrid(Action<GridView> configure) => configure(_view);

    public void BindRows(IEnumerable<TItem> rows)
    {
        _rows.Clear();
        foreach (TItem row in rows)
        {
            _rows.Add(row);
        }
    }

    public void ShowBusy(string message)
    {
        if (_overlay is not null)
        {
            return;
        }

        OverlayWindowOptions options = new()
        {
            FadeIn = false,
            FadeOut = false,
            // No bloquear controles: solo indicador visual.
            DisableInput = false,
            // Evita parpadeo cuando la consulta es muy rápida; si tarda, aparece inmediatamente.
            StartupDelay = 50,
            TrackOwnerBounds = true,
            ForceUseSkinLoadingElement = false,
            AnimationType = WaitAnimationType.Line,
            BackColor = Color.FromArgb(30, 0, 0, 0),
            ForeColor = Color.White,
            Opacity = 0.28
        };

        _overlay = SplashScreenManager.ShowOverlayForm(_grid, options);
        UseWaitCursor = true;
    }

    public void HideBusy()
    {
        UseWaitCursor = false;
        if (_overlay is null)
        {
            return;
        }

        try
        {
            SplashScreenManager.CloseOverlayForm(_overlay);
        }
        finally
        {
            _overlay = null;
        }
    }

    public void ExportToXlsx(string path) => _grid.ExportToXlsx(path);
    public void ExportToCsv(string path) => _grid.ExportToCsv(path);

    public string FindText
    {
        get => _view.FindFilterText;
        set => _view.FindFilterText = value ?? string.Empty;
    }

    public bool ShowFilterRow
    {
        get => _view.OptionsView.ShowAutoFilterRow;
        set => _view.OptionsView.ShowAutoFilterRow = value;
    }

    public void ApplyFindText(string text)
    {
        string value = text ?? string.Empty;
        _view.FindFilterText = value;
        // fuerza refresco del filtro del find panel aunque esté oculto
        _view.ApplyFindFilter(value);
    }

    public TItem? GetFocusedItem()
    {
        int handle = _view.FocusedRowHandle;
        return handle >= 0 ? _view.GetRow(handle) as TItem : null;
    }
}
