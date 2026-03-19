using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class NotesWidgetControl : XtraUserControl
{
    public NotesWidgetControl()
    {
        Dock = DockStyle.Fill;

        GroupControl group = new()
        {
            Text = "Notas",
            Dock = DockStyle.Fill
        };

        MemoEdit notes = new()
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                ReadOnly = false,
                NullValuePrompt = "Escribe observaciones de trabajo para este registro...",
                NullValuePromptShowForEmptyValue = true
            }
        };

        group.Controls.Add(notes);
        Controls.Add(group);
    }
}

