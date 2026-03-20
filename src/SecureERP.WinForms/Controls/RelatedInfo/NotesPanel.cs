using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class NotesPanel : XtraUserControl
{
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }

    private readonly MemoEdit _notes;

    public NotesPanel()
    {
        Dock = DockStyle.Fill;
        GroupControl group = new()
        {
            Text = "Notas",
            Dock = DockStyle.Fill
        };

        _notes = new MemoEdit
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                NullValuePrompt = "Escribe observaciones...",
                NullValuePromptShowForEmptyValue = true
            }
        };

        group.Controls.Add(_notes);
        Controls.Add(group);
    }

    public string NotesText
    {
        get => _notes.Text;
        set => _notes.Text = value;
    }
}
