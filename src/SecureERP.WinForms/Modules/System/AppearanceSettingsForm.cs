using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.XtraEditors;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms.Modules.System;

public sealed class AppearanceSettingsForm : XtraForm
{
    private readonly IThemePreferenceService _themePreferenceService;
    private readonly ListBoxControl _skinsList;
    private readonly LabelControl _currentSkinLabel;

    public AppearanceSettingsForm(IThemePreferenceService themePreferenceService)
    {
        _themePreferenceService = themePreferenceService;

        Text = "Apariencia";
        MinimumSize = new Size(900, 600);

        SplitContainerControl split = new()
        {
            Dock = DockStyle.Fill,
            SplitterPosition = 300
        };

        GroupControl skinsGroup = new()
        {
            Text = "Skins disponibles",
            Dock = DockStyle.Fill
        };

        _skinsList = new ListBoxControl
        {
            Dock = DockStyle.Fill
        };

        foreach (SkinContainer skin in SkinManager.Default.Skins)
        {
            _skinsList.Items.Add(skin.SkinName);
        }

        skinsGroup.Controls.Add(_skinsList);

        GroupControl previewGroup = new()
        {
            Text = "Vista previa",
            Dock = DockStyle.Fill
        };

        PanelControl previewRoot = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
            Padding = new Padding(16)
        };

        _currentSkinLabel = new LabelControl
        {
            Dock = DockStyle.Top,
            Height = 28,
            Appearance = { Font = new Font("Segoe UI", 10, FontStyle.Bold) }
        };

        LabelControl helper = new()
        {
            Text = "Selecciona un skin y aplica cambios. La preferencia queda persistida localmente.",
            Dock = DockStyle.Top,
            Height = 48,
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };

        SimpleButton applyButton = new()
        {
            Text = "Aplicar y guardar",
            Width = 170,
            Height = 38,
            Dock = DockStyle.Top
        };

        applyButton.Click += (_, _) => ApplySelectedSkin();

        previewRoot.Controls.Add(new PanelControl
        {
            Dock = DockStyle.Top,
            Height = 8,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        });
        previewRoot.Controls.Add(applyButton);
        previewRoot.Controls.Add(helper);
        previewRoot.Controls.Add(_currentSkinLabel);

        previewGroup.Controls.Add(previewRoot);

        split.Panel1.Controls.Add(skinsGroup);
        split.Panel2.Controls.Add(previewGroup);

        Controls.Add(split);

        LoadCurrentSkinSelection();

        _skinsList.SelectedIndexChanged += (_, _) => PreviewSelectedSkin();
    }

    private void LoadCurrentSkinSelection()
    {
        string currentSkin = UserLookAndFeel.Default.SkinName;
        _currentSkinLabel.Text = $"Skin actual: {currentSkin}";

        int index = _skinsList.Items.IndexOf(currentSkin);
        if (index >= 0)
        {
            _skinsList.SelectedIndex = index;
        }
    }

    private void PreviewSelectedSkin()
    {
        if (_skinsList.SelectedItem is not string skin)
        {
            return;
        }

        UserLookAndFeel.Default.SetSkinStyle(skin);
        _currentSkinLabel.Text = $"Skin actual: {skin}";
    }

    private void ApplySelectedSkin()
    {
        if (_skinsList.SelectedItem is not string skin)
        {
            return;
        }

        UserLookAndFeel.Default.SetSkinStyle(skin);
        _themePreferenceService.Save(new ThemePreference(skin));
        XtraMessageBox.Show(this, "Preferencia de apariencia guardada.", "SecureERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}


