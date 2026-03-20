using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using SecureERP.WinForms.Common;
using SecureERP.WinForms.Controls.RelatedInfo;
using SecureERP.WinForms.Services.Search;
using SecureERP.WinForms.Services.Workspace;
using System.Threading;

namespace SecureERP.WinForms.Search;

/// <summary>
/// Base reusable tipo NAV/Business Central para páginas de lista/búsqueda:
/// - búsqueda integrada al Grid (Find Panel)
/// - comandos en Ribbon (no botones locales)
/// - paginación/orden/filtros gobernados por Ribbon
/// </summary>
public abstract class SearchWorkspacePageBase<TItem> : XtraUserControl, IListWorkspacePage<TItem> where TItem : class
{
    public abstract string PageKey { get; }
    public abstract string Title { get; }
    public Control MainControl => this;
    public virtual Control? AuxiliaryPanel => ShowRelatedInfo ? _relatedSidebar : null;

    public IReadOnlyList<TItem> Items => _items.ToArray();
    public TItem? SelectedItem => Results.GetFocusedItem();

    public string SearchText
    {
        get => Results.FindText;
        set => Results.FindText = value;
    }

    public string? SelectedFilter
    {
        get => _quickFilter;
        set
        {
            _quickFilter = value;
            _page = 1;
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            _pageSize = Math.Max(5, value);
            _page = 1;
        }
    }

    public int CurrentPage => _page;
    public int TotalPages => _totalPages;
    public bool IsBusy => _isBusy;
    public string? BusyMessage => _busyMessage;
    public string? EntityType => _contextEntityType;
    public long? EntityId => _contextEntityId;
    public bool HasSelection => SelectedItem is not null;

    public event EventHandler? ContextChanged;
    public event EventHandler? LoadingStateChanged;

    protected SearchResultsWorkspaceControl<TItem> Results { get; }
    protected RelatedInfoSidebarControl RelatedSidebar => _relatedSidebar;

    private readonly RelatedInfoSidebarControl _relatedSidebar;
    private readonly System.Windows.Forms.Timer _debounce;
    private readonly List<TItem> _items = [];

    private BarButtonItem? _cmdNew;
    private BarButtonItem? _cmdEdit;
    private BarButtonItem? _cmdClose;
    private BarButtonItem? _cmdSearch;
    private BarButtonItem? _cmdRefresh;
    private BarButtonItem? _cmdClear;
    private BarButtonItem? _cmdExport;
    private BarButtonItem? _cmdClearSort;
    private BarButtonItem? _cmdAdvancedFilter;
    private BarButtonItem? _cmdPrev;
    private BarButtonItem? _cmdNext;
    private BarStaticItem? _pageInfo;
    private BarCheckItem? _chkFilterRow;
    private BarEditItem? _editSearchText;
    private RepositoryItemTextEdit? _searchRepo;
    private BarEditItem? _editQuickFilter;
    private RepositoryItemComboBox? _quickFilterRepo;
    private BarEditItem? _editPageSize;
    private RepositoryItemSpinEdit? _pageSizeRepo;
    private BarEditItem? _editSortBy;
    private RepositoryItemComboBox? _sortRepo;
    private BarCheckItem? _chkSortDesc;

    private int _page = 1;
    private int _totalPages = 1;
    private int _pageSize = UiConstants.DefaultPageSize;
    private string? _quickFilter = "Todos";
    private string? _sortBy;
    private bool _sortDesc;
    private bool _isBusy;
    private string? _busyMessage;
    private string? _contextEntityType;
    private long? _contextEntityId;
    private CancellationTokenSource? _cts;
    private long _searchVersion;

    protected SearchWorkspacePageBase()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(0);

        Results = new SearchResultsWorkspaceControl<TItem>();
        Results.ConfigureGrid(ConfigureGrid);
        Results.SelectionChanged += (_, _) => HandleSelectionChanged();

        // Cambios en filtros/orden disparan nueva consulta (debounce).
        Results.View.ColumnFilterChanged += (_, _) => DebounceSearch(reset: true);
        Results.View.EndSorting += (_, _) => DebounceSearch(reset: true);

        _relatedSidebar = new RelatedInfoSidebarControl
        {
            Dock = DockStyle.Fill,
            Width = 320
        };

        PanelControl root = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };
        root.Controls.Add(Results);
        Controls.Add(root);

        _debounce = new System.Windows.Forms.Timer { Interval = 350 };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await ExecuteSearchAsync(resetPage: false);
        };
    }

    protected virtual bool ShowRelatedInfo => true;
    protected virtual bool AutoSearchOnLoad => true;
    protected virtual bool AllowCreate => false;
    protected virtual bool AllowEdit => false;

    public void BuildRibbon(RibbonPage page)
    {
        page.Groups.Clear();

        RibbonControl ribbon = page.Ribbon;

        _cmdNew ??= CreateButton(ribbon, "Nuevo", (_, _) => OnNewRequested());
        _cmdEdit ??= CreateButton(ribbon, "Editar", (_, _) => OnEditRequested());
        _cmdClose ??= CreateButton(ribbon, "Cerrar", (_, _) => OnCloseRequested());
        _cmdSearch ??= CreateButton(ribbon, "Buscar", async (_, _) => await ExecuteSearchAsync(resetPage: true));
        _cmdRefresh ??= CreateButton(ribbon, "Refrescar", async (_, _) => await ExecuteSearchAsync(resetPage: false));
        _cmdClear ??= CreateButton(ribbon, "Limpiar", (_, _) => ClearAll());
        _cmdExport ??= CreateButton(ribbon, "Exportar", (_, _) => ExportGrid());
        _cmdClearSort ??= CreateButton(ribbon, "Limpiar orden", (_, _) => ClearSort());
        _cmdAdvancedFilter ??= CreateButton(ribbon, "Filtro avanzado", (_, _) => ShowAdvancedFilter());

        _cmdPrev ??= CreateButton(ribbon, "Anterior", async (_, _) => await GoPrev());
        _cmdNext ??= CreateButton(ribbon, "Siguiente", async (_, _) => await GoNext());
        _pageInfo ??= new BarStaticItem { Caption = PageInfoCaption() };
        AddItemIfNeeded(ribbon, _pageInfo);

        _chkFilterRow ??= new BarCheckItem { Caption = "Fila de filtros" };
        AddItemIfNeeded(ribbon, _chkFilterRow);

        _chkFilterRow.CheckedChanged -= OnFilterRowToggle;
        _chkFilterRow.Checked = Results.ShowFilterRow;
        _chkFilterRow.CheckedChanged += OnFilterRowToggle;

        _searchRepo ??= new RepositoryItemTextEdit();
        _editSearchText ??= new BarEditItem { Caption = "Buscar", Edit = _searchRepo, Width = 220 };
        AddItemIfNeeded(ribbon, _editSearchText);
        _editSearchText.EditValueChanged -= OnSearchTextChanged;
        _editSearchText.EditValue = Results.FindText;
        _editSearchText.EditValueChanged += OnSearchTextChanged;

        _quickFilterRepo ??= new RepositoryItemComboBox { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor };
        if (_quickFilterRepo.Items.Count == 0)
        {
            foreach (string option in GetQuickFilterOptions())
            {
                _quickFilterRepo.Items.Add(option);
            }
        }
        _editQuickFilter ??= new BarEditItem { Caption = "Filtro", Edit = _quickFilterRepo, Width = 160 };
        AddItemIfNeeded(ribbon, _editQuickFilter);
        _editQuickFilter.EditValueChanged -= OnQuickFilterChanged;
        _editQuickFilter.EditValue = _quickFilter ?? GetQuickFilterOptions().FirstOrDefault();
        _editQuickFilter.EditValueChanged += OnQuickFilterChanged;

        _pageSizeRepo ??= new RepositoryItemSpinEdit
        {
            IsFloatValue = false,
            MinValue = 5,
            MaxValue = 500,
            Increment = 5
        };
        _editPageSize ??= new BarEditItem { Caption = "Tamaño", Edit = _pageSizeRepo, Width = 90 };
        AddItemIfNeeded(ribbon, _editPageSize);
        _editPageSize.EditValueChanged -= OnPageSizeChanged;
        _editPageSize.EditValue = _pageSize;
        _editPageSize.EditValueChanged += OnPageSizeChanged;

        _sortRepo ??= new RepositoryItemComboBox { TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor };
        if (_sortRepo.Items.Count == 0)
        {
            foreach (SortOption option in GetSortOptions())
            {
                _sortRepo.Items.Add(option.Key);
            }
        }
        _editSortBy ??= new BarEditItem { Caption = "Orden", Edit = _sortRepo, Width = 140 };
        AddItemIfNeeded(ribbon, _editSortBy);
        _editSortBy.EditValueChanged -= OnSortByChanged;
        _editSortBy.EditValue = _sortBy ?? GetSortOptions().FirstOrDefault()?.Key;
        _editSortBy.EditValueChanged += OnSortByChanged;

        _chkSortDesc ??= new BarCheckItem { Caption = "Desc" };
        AddItemIfNeeded(ribbon, _chkSortDesc);
        _chkSortDesc.CheckedChanged -= OnSortDirChanged;
        _chkSortDesc.Checked = _sortDesc;
        _chkSortDesc.CheckedChanged += OnSortDirChanged;

        RibbonPageGroup record = new("Registro");
        record.ItemLinks.Add(_cmdNew);
        record.ItemLinks.Add(_cmdEdit);
        record.ItemLinks.Add(_cmdClose);

        RibbonPageGroup query = new("Consulta");
        query.ItemLinks.Add(_editSearchText);
        query.ItemLinks.Add(_cmdSearch);
        query.ItemLinks.Add(_cmdRefresh);
        query.ItemLinks.Add(_cmdClear);
        query.ItemLinks.Add(_editQuickFilter);

        RibbonPageGroup view = new("Vista");
        view.ItemLinks.Add(_chkFilterRow);
        view.ItemLinks.Add(_cmdClearSort);
        view.ItemLinks.Add(_cmdAdvancedFilter);

        RibbonPageGroup export = new("Exportación");
        export.ItemLinks.Add(_cmdExport);

        RibbonPageGroup paging = new("Página");
        paging.ItemLinks.Add(_cmdPrev);
        paging.ItemLinks.Add(_cmdNext);
        paging.ItemLinks.Add(_editPageSize);
        paging.ItemLinks.Add(_editSortBy);
        paging.ItemLinks.Add(_chkSortDesc);
        paging.ItemLinks.Add(_pageInfo);

        page.Groups.Add(record);
        page.Groups.Add(query);
        page.Groups.Add(view);
        page.Groups.Add(export);
        page.Groups.Add(paging);

        UpdateRibbonState();
    }

    public async void OnActivated()
    {
        UpdateRibbonState();
        try
        {
            if (AutoSearchOnLoad && Results.View.DataRowCount == 0)
            {
                await ExecuteSearchAsync(resetPage: true);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task RefreshAsync(bool resetPage = false, CancellationToken cancellationToken = default)
        => await ExecuteSearchAsync(resetPage, cancellationToken);

    protected abstract Task<SearchPageResult<TItem>> ExecuteSearchCoreAsync(SearchQuery query, CancellationToken cancellationToken);
    protected abstract void ConfigureGrid(DevExpress.XtraGrid.Views.Grid.GridView view);

    protected virtual IReadOnlyList<string> GetQuickFilterOptions() => ["Todos", "Activo", "Suspendido", "Borrador"];
    protected virtual IReadOnlyList<SortOption> GetSortOptions() => [new("Code", "Código")];
    protected virtual void OnSelectionChanged(TItem? item) { }
    protected virtual string? ResolveContextEntityType(TItem? item) => null;
    protected virtual long? ResolveContextEntityId(TItem? item) => null;

    protected virtual void OnOpenDetail(TItem item)
    {
        XtraMessageBox.Show(this, $"Detalle no implementado para {item}", "Detalle", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected virtual void OnNewRequested()
    {
        XtraMessageBox.Show(this, "Nuevo no está habilitado en esta página.", "SecureERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected virtual void OnEditRequested()
    {
        if (SelectedItem is null)
        {
            return;
        }

        XtraMessageBox.Show(this, "Editar no está habilitado en esta página.", "SecureERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnCloseRequested()
    {
        FindForm()?.Close();
    }

    private void DebounceSearch(bool reset)
    {
        if (reset)
        {
            _page = 1;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    private async Task GoPrev()
    {
        if (_page <= 1)
        {
            return;
        }

        _page--;
        await ExecuteSearchAsync(resetPage: false);
    }

    private async Task GoNext()
    {
        if (_page >= _totalPages)
        {
            return;
        }

        _page++;
        await ExecuteSearchAsync(resetPage: false);
    }

    private async Task ExecuteSearchAsync(bool resetPage, CancellationToken cancellationToken = default)
    {
        if (resetPage)
        {
            _page = 1;
        }

        // Versionado para evitar carreras: una busqueda vieja no debe apagar el loading de una busqueda nueva
        // ni sobrescribir resultados mas recientes.
        long version = Interlocked.Increment(ref _searchVersion);

        CancellationTokenSource? oldCts = _cts;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        oldCts?.Cancel();
        oldCts?.Dispose();

        string text = Results.FindText?.Trim() ?? string.Empty;
        string filter = _quickFilter ?? string.Empty;
        int pageSize = _pageSize;
        string? advancedFilter = Results.View.ActiveFilterString;

        SetBusy(true, "Cargando resultados...");
        UpdateRibbonState();
        try
        {
            // Permite que el UI pinte el overlay antes de iniciar el trabajo (paginacion/refresh).
            await Task.Yield();

            SearchQuery query = new(text, filter, _page, pageSize, _sortBy, _sortDesc, advancedFilter);
            // Ejecuta la consulta fuera del hilo UI para evitar congelamiento y asegurar que el loading sea visible.
            SearchPageResult<TItem> result = await Task.Run(
                () => ExecuteSearchCoreAsync(query, _cts.Token),
                _cts.Token);

            // Si ya existe una busqueda mas reciente, ignoramos este resultado.
            if (version != Interlocked.Read(ref _searchVersion))
            {
                return;
            }

            _items.Clear();
            _items.AddRange(result.Items);
            Results.BindRows(result.Items);
            _page = result.Page;
            _totalPages = result.TotalPages;
            HandleSelectionChanged();
            UpdateRibbonState();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            // Solo el ultimo request debe apagar el indicador.
            if (version == Interlocked.Read(ref _searchVersion))
            {
                SetBusy(false, null);
                UpdateRibbonState();
            }
        }
    }

    private void ClearAll()
    {
        Results.FindText = string.Empty;
        Results.ApplyFindText(string.Empty);
        if (_editSearchText is not null)
        {
            _editSearchText.EditValue = string.Empty;
        }
        Results.View.ActiveFilterString = string.Empty;
        _quickFilter = GetQuickFilterOptions().FirstOrDefault() ?? "Todos";
        if (_editQuickFilter is not null)
        {
            _editQuickFilter.EditValue = _quickFilter;
        }
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void ClearSort()
    {
        Results.View.SortInfo.Clear();
        Results.View.ClearSorting();
        _sortBy = GetSortOptions().FirstOrDefault()?.Key;
        _sortDesc = false;
        if (_editSortBy is not null) _editSortBy.EditValue = _sortBy;
        if (_chkSortDesc is not null) _chkSortDesc.Checked = _sortDesc;
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void ShowAdvancedFilter()
    {
        // Experiencia tipo NAV: el usuario define un filtro avanzado por columnas.
        Results.View.ShowFilterEditor(null);
    }

    private void ExportGrid()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            FileName = $"{Title.Replace(' ', '_')}.xlsx"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        if (dialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            Results.ExportToCsv(dialog.FileName);
        }
        else
        {
            Results.ExportToXlsx(dialog.FileName);
        }
    }

    private void HandleSelectionChanged()
    {
        TItem? item = SelectedItem;
        _contextEntityType = ResolveContextEntityType(item);
        _contextEntityId = ResolveContextEntityId(item);
        ContextChanged?.Invoke(this, EventArgs.Empty);
        OnSelectionChanged(item);
        UpdateRibbonState();
    }

    private void SetBusy(bool busy, string? message)
    {
        _isBusy = busy;
        _busyMessage = message;
        if (busy)
        {
            Results.ShowBusy(message ?? "Procesando...");
        }
        else
        {
            Results.HideBusy();
        }

        LoadingStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateRibbonState()
    {
        bool hasRows = Results.View.DataRowCount > 0;
        if (_cmdEdit is not null) _cmdEdit.Enabled = AllowEdit && hasRows;
        if (_cmdNew is not null) _cmdNew.Enabled = AllowCreate;
        if (_cmdExport is not null) _cmdExport.Enabled = hasRows;
        if (_cmdPrev is not null) _cmdPrev.Enabled = _page > 1;
        if (_cmdNext is not null) _cmdNext.Enabled = _page < _totalPages;
        if (_cmdAdvancedFilter is not null) _cmdAdvancedFilter.Enabled = true;
        if (_cmdSearch is not null) _cmdSearch.Enabled = true;
        if (_cmdRefresh is not null) _cmdRefresh.Enabled = true;
        if (_cmdClear is not null) _cmdClear.Enabled = true;
        if (_cmdClose is not null) _cmdClose.Enabled = true;
        if (_editSearchText is not null) _editSearchText.Enabled = true;
        if (_editQuickFilter is not null) _editQuickFilter.Enabled = true;
        if (_editPageSize is not null) _editPageSize.Enabled = true;

        if (_pageInfo is not null)
        {
            _pageInfo.Caption = PageInfoCaption();
        }
    }

    private string PageInfoCaption() => $"Página {_page} de {_totalPages}";

    private void OnFilterRowToggle(object? sender, ItemClickEventArgs e)
    {
        Results.ShowFilterRow = _chkFilterRow?.Checked ?? false;
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        string text = _editSearchText?.EditValue?.ToString() ?? string.Empty;
        Results.FindText = text;
        Results.ApplyFindText(text);
        DebounceSearch(reset: true);
    }

    private void OnQuickFilterChanged(object? sender, EventArgs e)
    {
        _quickFilter = _editQuickFilter?.EditValue?.ToString() ?? "Todos";
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        int size = _pageSize;
        if (_editPageSize?.EditValue is int i)
        {
            size = i;
        }
        else if (int.TryParse(_editPageSize?.EditValue?.ToString(), out int parsed))
        {
            size = parsed;
        }

        _pageSize = Math.Max(5, size);
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void OnSortByChanged(object? sender, EventArgs e)
    {
        _sortBy = _editSortBy?.EditValue?.ToString();
        ApplySortToGrid();
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void OnSortDirChanged(object? sender, ItemClickEventArgs e)
    {
        _sortDesc = _chkSortDesc?.Checked ?? false;
        ApplySortToGrid();
        _page = 1;
        _ = ExecuteSearchAsync(resetPage: true);
    }

    private void ApplySortToGrid()
    {
        if (string.IsNullOrWhiteSpace(_sortBy))
        {
            return;
        }

        DevExpress.XtraGrid.Columns.GridColumn? column = Results.View.Columns[_sortBy];
        if (column is null)
        {
            return;
        }

        Results.View.SortInfo.Clear();
        column.SortOrder = _sortDesc
            ? DevExpress.Data.ColumnSortOrder.Descending
            : DevExpress.Data.ColumnSortOrder.Ascending;
    }

    private BarButtonItem CreateButton(RibbonControl ribbon, string caption, ItemClickEventHandler handler)
    {
        BarButtonItem item = new() { Caption = caption };
        item.ItemClick += handler;
        AddItemIfNeeded(ribbon, item);
        return item;
    }

    private static void AddItemIfNeeded(RibbonControl ribbon, BarItem item)
    {
        if (!ribbon.Items.Contains(item))
        {
            ribbon.Items.Add(item);
        }
    }

    protected sealed record SortOption(string Key, string Caption);
}
