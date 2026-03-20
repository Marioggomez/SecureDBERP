using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class RelatedDocumentsPanel : XtraUserControl
{
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }

    private readonly ListBoxControl _list;

    public RelatedDocumentsPanel()
    {
        Dock = DockStyle.Fill;
        GroupControl group = new()
        {
            Text = "Documentos relacionados",
            Dock = DockStyle.Fill
        };

        _list = new ListBoxControl
        {
            Dock = DockStyle.Fill
        };

        group.Controls.Add(_list);
        Controls.Add(group);
    }

    public void BindDocuments(IEnumerable<string> docs)
    {
        _list.Items.Clear();
        _list.Items.AddRange(docs.ToArray());
    }
}
