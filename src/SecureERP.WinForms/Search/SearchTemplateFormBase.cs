using System.ComponentModel;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraWaitForm;
using SecureERP.WinForms.Common;
using SecureERP.WinForms.Controls.RelatedInfo;
using SecureERP.WinForms.Infrastructure.Ui;
using SecureERP.WinForms.Services.Search;

namespace SecureERP.WinForms.Search;

public abstract class SearchTemplateFormBase<TItem> : XtraForm where TItem : class
{
    private readonly SearchControl _searchControl;
    private readonly ComboBoxEdit _filterCombo;
    private readonly ComboBoxEdit _pageSizeCombo;
    private readonly SimpleButton _searchButton;
    private readonly SimpleButton _prevButton;
    private readonly SimpleButton _nextButton;
    private readonly LabelControl _pagingLabel;
    private readonly GridControl _grid;
    private readonly GridView _view;
    private readonly PanelControl _loadingOverlay;
    private readonly LabelControl _loadingMessage;
    private readonly ProgressPanel _loadingProgress;
    private readonly BusyOverlayController _busyOverlay;
    private readonly BindingList<TItem> _rows;
    private readonly System.Windows.Forms.Timer _debounceTimer;

    private int _currentPage = 1;
    private int _totalPages = 1;
    private CancellationTokenSource? _searchCts;

    protected SearchTemplateFormBase(string caption)
    {
        Text = caption;
        MinimumSize = new Size(1100, 700);

        _rows = [];

        PanelControl searchPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 62,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(10)
        };

        _searchControl = new SearchControl
        {
            Width = 360,
            Properties =
            {
                NullValuePrompt = "Buscar por código, nombre o categoría",
                NullValuePromptShowForEmptyValue = true
            }
        };

        _filterCombo = new ComboBoxEdit
        {
            Width = 170
        };
        _filterCombo.Properties.Items.AddRange(GetFilterValues().ToArray());
        _filterCombo.SelectedIndex = 0;

        _pageSizeCombo = new ComboBoxEdit
        {
            Width = 90
        };
        _pageSizeCombo.Properties.Items.AddRange(["25", "50", "100"]);
        _pageSizeCombo.SelectedIndex = 0;

        _searchButton = new SimpleButton
        {
            Text = "Buscar",
            Width = 100
        };

        FlowLayoutPanel searchFlow = new()
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = false
        };

        searchFlow.Controls.Add(_searchControl);
        searchFlow.Controls.Add(_filterCombo);
        searchFlow.Controls.Add(_pageSizeCombo);
        searchFlow.Controls.Add(_searchButton);

        searchPanel.Controls.Add(searchFlow);

        SplitContainerControl split = new()
        {
            Dock = DockStyle.Fill,
            SplitterPosition = 780
        };

        PanelControl resultsPanel = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };

        _grid = new GridControl
        {
            Dock = DockStyle.Fill
        };
        _view = new GridView(_grid)
        {
            OptionsBehavior = { Editable = false, ReadOnly = true },
            OptionsView = { ShowAutoFilterRow = false, ShowGroupPanel = false },
            OptionsSelection = { MultiSelect = false }
        };
        _grid.MainView = _view;
        _grid.DataSource = _rows;

        PanelControl pagingPanel = new()
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(10, 5, 10, 5)
        };

        FlowLayoutPanel pagingFlow = new()
        {
            Dock = DockStyle.Right,
            WrapContents = false,
            AutoSize = true
        };

        _prevButton = new SimpleButton { Text = "Anterior", Width = 90 };
        _nextButton = new SimpleButton { Text = "Siguiente", Width = 90 };
        _pagingLabel = new LabelControl { AutoSizeMode = LabelAutoSizeMode.None, Width = 180, Text = "Página 1 de 1" };

        pagingFlow.Controls.Add(_prevButton);
        pagingFlow.Controls.Add(_nextButton);
        pagingFlow.Controls.Add(_pagingLabel);

        pagingPanel.Controls.Add(pagingFlow);

        _loadingOverlay = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            BackColor = Color.FromArgb(245, 245, 245)
        };

        _loadingProgress = new ProgressPanel
        {
            Size = new Size(260, 70),
            Caption = "Cargando resultados",
            Description = "Espere mientras se consulta la información"
        };

        _loadingMessage = new LabelControl
        {
            Text = "Consultando información...",
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };

        _loadingOverlay.Controls.Add(_loadingProgress);
        _loadingOverlay.Controls.Add(_loadingMessage);

        _busyOverlay = new BusyOverlayController(_loadingOverlay, _loadingMessage, _loadingProgress);

        resultsPanel.Controls.Add(_grid);
        resultsPanel.Controls.Add(_loadingOverlay);
        resultsPanel.Controls.Add(pagingPanel);

        split.Panel1.Controls.Add(resultsPanel);
        split.Panel2.Controls.Add(new RelatedInfoPanelControl());

        Controls.Add(split);
        Controls.Add(searchPanel);

        ConfigureGrid(_view);

        _debounceTimer = new System.Windows.Forms.Timer
        {
            Interval = 400
        };

        WireEvents();
        PositionLoadingElements();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshResultsAsync(resetPage: true);
    }

    protected abstract Task<SearchPageResult<TItem>> ExecuteSearchAsync(SearchQuery query, CancellationToken cancellationToken);

    protected abstract void ConfigureGrid(GridView view);

    protected virtual IReadOnlyList<string> GetFilterValues() => ["Todos", "Activo", "Suspendido", "Borrador"];

    private void WireEvents()
    {
        _searchButton.Click += async (_, _) => await RefreshResultsAsync(resetPage: true);
        _prevButton.Click += async (_, _) =>
        {
            if (_currentPage <= 1)
            {
                return;
            }

            _currentPage--;
            await RefreshResultsAsync(resetPage: false);
        };

        _nextButton.Click += async (_, _) =>
        {
            if (_currentPage >= _totalPages)
            {
                return;
            }

            _currentPage++;
            await RefreshResultsAsync(resetPage: false);
        };

        _filterCombo.SelectedIndexChanged += async (_, _) => await RefreshResultsAsync(resetPage: true);
        _pageSizeCombo.SelectedIndexChanged += async (_, _) => await RefreshResultsAsync(resetPage: true);

        _searchControl.EditValueChanged += (_, _) =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        };

        _debounceTimer.Tick += async (_, _) =>
        {
            _debounceTimer.Stop();
            await RefreshResultsAsync(resetPage: true);
        };

        Resize += (_, _) => PositionLoadingElements();
    }

    private async Task RefreshResultsAsync(bool resetPage)
    {
        if (resetPage)
        {
            _currentPage = 1;
        }

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        string text = _searchControl.Text?.Trim() ?? string.Empty;
        string? filter = _filterCombo.SelectedItem?.ToString();

        int pageSize = UiConstants.DefaultPageSize;
        if (int.TryParse(_pageSizeCombo.SelectedItem?.ToString(), out int selectedSize) && selectedSize > 0)
        {
            pageSize = selectedSize;
        }

        SearchQuery query = new(text, filter, _currentPage, pageSize);

        await _busyOverlay.RunAsync(async () =>
        {
            SearchPageResult<TItem> result = await ExecuteSearchAsync(query, _searchCts.Token);
            _rows.Clear();
            foreach (TItem row in result.Items)
            {
                _rows.Add(row);
            }

            _totalPages = result.TotalPages;
            _currentPage = result.Page;
            _pagingLabel.Text = $"Página {_currentPage} de {_totalPages} | Registros: {result.TotalRecords}";
            _prevButton.Enabled = _currentPage > 1;
            _nextButton.Enabled = _currentPage < _totalPages;
        }, "Ejecutando búsqueda...");
    }

    private void PositionLoadingElements()
    {
        int centerX = _loadingOverlay.Width / 2;
        int centerY = _loadingOverlay.Height / 2;

        _loadingProgress.Left = Math.Max(8, centerX - (_loadingProgress.Width / 2));
        _loadingProgress.Top = Math.Max(8, centerY - (_loadingProgress.Height / 2) - 12);

        _loadingMessage.Left = Math.Max(8, centerX - (_loadingMessage.Width / 2));
        _loadingMessage.Top = _loadingProgress.Bottom + 8;
    }
}

