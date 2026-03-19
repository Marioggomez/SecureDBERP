using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Controls.RelatedInfo;

public sealed class DocumentsWidgetControl : XtraUserControl
{
    public DocumentsWidgetControl()
    {
        Dock = DockStyle.Fill;

        GroupControl group = new()
        {
            Text = "Documentos",
            Dock = DockStyle.Fill
        };

        ListBoxControl list = new()
        {
            Dock = DockStyle.Fill
        };
        list.Items.AddRange([
            "Adjunto: Especificación técnica.pdf",
            "Adjunto: Cotización proveedor.xlsx"
        ]);

        group.Controls.Add(list);
        Controls.Add(group);
    }
}

