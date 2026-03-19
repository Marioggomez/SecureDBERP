using DevExpress.XtraEditors;
using DevExpress.XtraTab;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class RelatedInfoPanelControl : XtraUserControl
{
    public RelatedInfoPanelControl()
    {
        Dock = DockStyle.Fill;

        XtraTabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };

        XtraTabPage docsPage = new() { Text = "Documentos" };
        XtraTabPage notesPage = new() { Text = "Notas" };
        XtraTabPage tagsPage = new() { Text = "Etiquetas" };

        docsPage.Controls.Add(new DocumentsWidgetControl());
        notesPage.Controls.Add(new NotesWidgetControl());
        tagsPage.Controls.Add(new TagsWidgetControl());

        tabs.TabPages.AddRange([docsPage, notesPage, tagsPage]);
        Controls.Add(tabs);
    }
}
