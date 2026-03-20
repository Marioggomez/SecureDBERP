using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace SecureERP.Desktop.Modules.Shared.Pages;

public abstract class ErpListPageBase : XtraUserControl
{
    private readonly PanelControl _toolbarPanel;
    private readonly LabelControl _titleLabel;
    private readonly SimpleButton _refreshButton;
    private bool _isLoaded;

    protected ErpListPageBase(string title)
    {
        Dock = DockStyle.Fill;

        _toolbarPanel = new PanelControl
        {
            Dock = DockStyle.Top,
            Height = 52
        };

        _titleLabel = new LabelControl
        {
            Text = title,
            Dock = DockStyle.Left,
            AutoSizeMode = LabelAutoSizeMode.None,
            Width = 420,
            Padding = new Padding(12, 16, 0, 0)
        };

        _refreshButton = new SimpleButton
        {
            Text = "Refrescar",
            Dock = DockStyle.Right,
            Width = 110,
            Padding = new Padding(0, 8, 8, 8)
        };
        _refreshButton.Click += async (_, _) => await ReloadAsync();

        Grid = new GridControl
        {
            Dock = DockStyle.Fill
        };

        GridView = new GridView(Grid)
        {
            OptionsView = { ShowAutoFilterRow = true, ShowGroupPanel = false },
            OptionsBehavior = { Editable = false }
        };
        Grid.MainView = GridView;

        _toolbarPanel.Controls.Add(_refreshButton);
        _toolbarPanel.Controls.Add(_titleLabel);
        Controls.Add(Grid);
        Controls.Add(_toolbarPanel);
    }

    protected GridControl Grid { get; }

    protected GridView GridView { get; }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_isLoaded || DesignMode)
        {
            return;
        }

        ConfigureGrid();
        _isLoaded = true;
        await ReloadAsync();
    }

    protected virtual void ConfigureGrid()
    {
    }

    protected abstract Task LoadDataAsync(CancellationToken cancellationToken = default);

    protected void BindData<T>(IEnumerable<T> items)
    {
        Grid.DataSource = items.ToList();
    }

    private async Task ReloadAsync()
    {
        _refreshButton.Enabled = false;
        try
        {
            await LoadDataAsync();
        }
        finally
        {
            _refreshButton.Enabled = true;
        }
    }
}
