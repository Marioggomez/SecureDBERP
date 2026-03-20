using DevExpress.XtraEditors;

namespace SecureERP.Desktop.Modules.Catalogs.Countries;

public sealed class CountryEditDialog : XtraForm
{
    public CountryEditDialog()
    {
        Text = "Pais";
        Width = 540;
        Height = 320;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new DevExpress.XtraLayout.LayoutControl { Dock = DockStyle.Fill };
        var code = new TextEdit();
        var name = new TextEdit();
        var iso3 = new TextEdit();
        var active = new CheckEdit { Text = "Activo" };

        var root = new DevExpress.XtraLayout.LayoutControlGroup();
        layout.Root = root;
        root.AddItem("Codigo", code);
        root.AddItem("Nombre", name);
        root.AddItem("ISO3", iso3);
        root.AddItem(string.Empty, active);

        var save = new SimpleButton { Text = "Guardar", Dock = DockStyle.Right, Width = 110 };
        save.Click += (_, _) => DialogResult = DialogResult.OK;

        var cancel = new SimpleButton { Text = "Cancelar", Dock = DockStyle.Right, Width = 110 };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var footer = new PanelControl { Dock = DockStyle.Bottom, Height = 52 };
        footer.Controls.Add(cancel);
        footer.Controls.Add(save);

        Controls.Add(layout);
        Controls.Add(footer);
    }
}