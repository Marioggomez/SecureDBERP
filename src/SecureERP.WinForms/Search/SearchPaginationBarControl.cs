using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Search;

/// <summary>
/// Barra reutilizable de paginación para búsquedas ERP.
/// </summary>
public sealed class SearchPaginationBarControl : XtraUserControl
{
    private readonly SimpleButton _prevButton;
    private readonly SimpleButton _nextButton;
    private readonly LabelControl _statusLabel;

    public event EventHandler? PreviousRequested;
    public event EventHandler? NextRequested;

    public SearchPaginationBarControl()
    {
        Dock = DockStyle.Bottom;
        Height = 44;
        Padding = new Padding(10, 5, 10, 5);

        FlowLayoutPanel flow = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false
        };

        _prevButton = new SimpleButton { Text = "Anterior", Width = 90 };
        _nextButton = new SimpleButton { Text = "Siguiente", Width = 90 };
        _statusLabel = new LabelControl
        {
            AutoSizeMode = LabelAutoSizeMode.None,
            Width = 200,
            Text = "Página 1 de 1"
        };

        _prevButton.Click += (_, _) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        _nextButton.Click += (_, _) => NextRequested?.Invoke(this, EventArgs.Empty);

        flow.Controls.Add(_prevButton);
        flow.Controls.Add(_nextButton);
        flow.Controls.Add(_statusLabel);
        Controls.Add(flow);
    }

    public void UpdateState(int page, int totalPages, bool canGoPrev, bool canGoNext)
    {
        _statusLabel.Text = $"Página {page} de {totalPages}";
        _prevButton.Enabled = canGoPrev;
        _nextButton.Enabled = canGoNext;
    }
}
