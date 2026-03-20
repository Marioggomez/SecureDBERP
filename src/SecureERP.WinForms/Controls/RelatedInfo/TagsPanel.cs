using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class TagsPanel : XtraUserControl
{
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }

    private readonly CheckedListBoxControl _tags;

    public TagsPanel()
    {
        Dock = DockStyle.Fill;
        GroupControl group = new()
        {
            Text = "Etiquetas",
            Dock = DockStyle.Fill
        };

        _tags = new CheckedListBoxControl
        {
            Dock = DockStyle.Fill
        };

        group.Controls.Add(_tags);
        Controls.Add(group);
    }

    public void BindTags(IEnumerable<string> tags)
    {
        _tags.Items.Clear();
        foreach (string tag in tags)
        {
            _tags.Items.Add(tag, false);
        }
    }
}
