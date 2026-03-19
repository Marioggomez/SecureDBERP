using DevExpress.XtraEditors;

namespace SecureERP.WinForms.Modules.Home;

public sealed class HomeDashboardForm : XtraForm
{
    public HomeDashboardForm()
    {
        Text = "Inicio";
        MinimumSize = new Size(800, 500);

        PanelControl root = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(24)
        };

        LabelControl title = new()
        {
            Text = "SecureERP Desktop",
            Appearance = { Font = new Font("Segoe UI", 24, FontStyle.Bold) },
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 56
        };

        LabelControl subtitle = new()
        {
            Text = "Framework base WinForms + DevExpress para módulos ERP empresariales.",
            Appearance = { Font = new Font("Segoe UI", 11, FontStyle.Regular) },
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.None,
            Height = 34
        };

        GroupControl quickStart = new()
        {
            Text = "Acciones iniciales",
            Dock = DockStyle.Top,
            Height = 220
        };

        MemoEdit notes = new()
        {
            Dock = DockStyle.Fill,
            Properties =
            {
                ReadOnly = true
            },
            Text = "1) Usa el menú izquierdo para abrir Búsqueda Global.\r\n" +
                   "2) Usa el menú Apariencia para cambiar skins y persistir preferencia.\r\n" +
                   "3) Extiende SearchTemplateFormBase para nuevos catálogos ERP.\r\n" +
                   "4) Reutiliza RelatedInfoPanelControl en formularios de negocio."
        };

        quickStart.Controls.Add(notes);

        root.Controls.Add(quickStart);
        root.Controls.Add(subtitle);
        root.Controls.Add(title);

        Controls.Add(root);
    }
}


