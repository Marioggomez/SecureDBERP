using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class TagsWidgetControl : XtraUserControl
{
    public TagsWidgetControl()
    {
        Dock = DockStyle.Fill;

        GroupControl group = new()
        {
            Text = "Etiquetas",
            Dock = DockStyle.Fill
        };

        CheckedListBoxControl tags = new()
        {
            Dock = DockStyle.Fill
        };

        tags.Items.Add("Urgente", false);
        tags.Items.Add("Requiere revisión", false);
        tags.Items.Add("En aprobación", true);

        group.Controls.Add(tags);
        Controls.Add(group);
    }
}

