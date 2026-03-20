using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using SecureERP.WinForms.Services.Workspace;

namespace SecureERP.WinForms.Controls.RelatedInfo;

/// <summary>
/// Panel lateral combinable que aloja documentos, notas y etiquetas para cualquier entidad.
/// </summary>
public sealed class RelatedInfoSidebarControl : XtraUserControl, IFactBoxPanel
{
    public string? EntityType
    {
        get => _entityType;
        set
        {
            _entityType = value;
            _docs.EntityType = value;
            _notes.EntityType = value;
            _tags.EntityType = value;
        }
    }

    public long? EntityId
    {
        get => _entityId;
        set
        {
            _entityId = value;
            _docs.EntityId = value;
            _notes.EntityId = value;
            _tags.EntityId = value;
        }
    }

    private string? _entityType;
    private long? _entityId;
    private readonly RelatedDocumentsPanel _docs;
    private readonly NotesPanel _notes;
    private readonly TagsPanel _tags;
    private readonly XtraTabControl _tabs;

    public RelatedInfoSidebarControl()
    {
        Dock = DockStyle.Fill;
        _docs = new RelatedDocumentsPanel();
        _notes = new NotesPanel();
        _tags = new TagsPanel();
        _tabs = new XtraTabControl
        {
            Dock = DockStyle.Fill
        };

        XtraTabPage docsPage = new() { Text = "Documentos" };
        XtraTabPage notesPage = new() { Text = "Notas" };
        XtraTabPage tagsPage = new() { Text = "Etiquetas" };

        docsPage.Controls.Add(_docs);
        notesPage.Controls.Add(_notes);
        tagsPage.Controls.Add(_tags);

        _tabs.TabPages.AddRange([docsPage, notesPage, tagsPage]);
        Controls.Add(_tabs);
    }

    public void BindDocuments(IEnumerable<string> docs) => _docs.BindDocuments(docs);
    public void BindTags(IEnumerable<string> tags) => _tags.BindTags(tags);
    public string NotesText { get => _notes.NotesText; set => _notes.NotesText = value; }

    public void BindContext(string? entityType, long? entityId)
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public void SelectTab(string key)
    {
        switch (key.ToLowerInvariant())
        {
            case "docs":
                _tabs.SelectedTabPageIndex = 0;
                break;
            case "notes":
                _tabs.SelectedTabPageIndex = 1;
                break;
            case "tags":
                _tabs.SelectedTabPageIndex = 2;
                break;
        }
    }
}
