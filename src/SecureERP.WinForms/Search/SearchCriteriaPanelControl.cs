using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Search;

/// <summary>
/// Criterios comunes reutilizables para pantallas de búsqueda ERP.
/// </summary>
public sealed class SearchCriteriaPanelControl : XtraUserControl
{
    private readonly SearchControl _searchBox;
    private readonly ComboBoxEdit _filterCombo;
    private readonly SpinEdit _pageSize;

    public event EventHandler? SearchChanged;
    public event EventHandler? FilterChanged;
    public event EventHandler? PageSizeChanged;

    public string SearchText
    {
        get => _searchBox.Text;
        set => _searchBox.Text = value;
    }

    public string? SelectedFilter
    {
        get => _filterCombo.SelectedItem?.ToString();
        set => _filterCombo.SelectedItem = value;
    }

    public int PageSize
    {
        get => (int)_pageSize.Value;
        set => _pageSize.Value = value;
    }

    public SearchCriteriaPanelControl()
    {
        Dock = DockStyle.Top;
        Height = 56;
        Padding = new Padding(10, 8, 10, 8);

        _searchBox = new SearchControl
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                NullValuePrompt = "Código, nombre o criterio",
                NullValuePromptShowForEmptyValue = true
            }
        };

        _filterCombo = new ComboBoxEdit
        {
            Dock = DockStyle.Right,
            Width = 160
        };

        _pageSize = new SpinEdit
        {
            Dock = DockStyle.Right,
            Width = 90,
            Properties =
            {
                IsFloatValue = false,
                MinValue = 10,
                MaxValue = 500,
                Increment = 10
            }
        };

        Controls.Add(_searchBox);
        Controls.Add(_filterCombo);
        Controls.Add(_pageSize);

        _searchBox.EditValueChanged += (_, _) => SearchChanged?.Invoke(this, EventArgs.Empty);
        _filterCombo.SelectedIndexChanged += (_, _) => FilterChanged?.Invoke(this, EventArgs.Empty);
        _pageSize.EditValueChanged += (_, _) => PageSizeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetFilterOptions(IEnumerable<string> filters)
    {
        _filterCombo.Properties.Items.Clear();
        _filterCombo.Properties.Items.AddRange(filters.ToArray());
        if (_filterCombo.Properties.Items.Count > 0 && _filterCombo.SelectedIndex < 0)
        {
            _filterCombo.SelectedIndex = 0;
        }
    }
}
