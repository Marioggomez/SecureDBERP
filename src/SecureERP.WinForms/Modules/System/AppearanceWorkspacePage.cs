using DevExpress.LookAndFeel;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Helpers;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using SecureERP.WinForms.Common;
using SecureERP.WinForms.Services.Workspace;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms.Modules.System;

public sealed class AppearanceWorkspacePage : XtraUserControl, IWorkspacePage
{
    public string PageKey => "SYSTEM.APPEARANCE";
    public string Title => "Apariencia";
    public Control MainControl => this;
    public Control? AuxiliaryPanel => null;
    public string? EntityType => null;
    public long? EntityId => null;
    public bool HasSelection => false;
    public bool IsBusy => false;
    public string? BusyMessage => null;

    public event EventHandler? ContextChanged { add { } remove { } }
    public event EventHandler? LoadingStateChanged { add { } remove { } }

    private readonly IThemePreferenceService _themePreferenceService;
    private readonly LabelControl _summaryLabel;
    private readonly LabelControl _currentSkinLabel;
    private readonly LabelControl _currentPaletteLabel;
    private readonly ColorPickEdit _accentColor;
    private readonly GroupControl _previewCard;
    private readonly LabelControl _previewTitle;
    private readonly LabelControl _previewBody;
    private readonly SkinRibbonGalleryBarItem _skinGallery;
    private readonly SkinPaletteRibbonGalleryBarItem _paletteGallery;
    private readonly BarButtonItem _cmdSave;
    private readonly BarButtonItem _cmdReset;
    private readonly BarCheckItem _chkRoundedCorners;
    private readonly BarCheckItem _chkCompactMode;

    public AppearanceWorkspacePage(IThemePreferenceService themePreferenceService)
    {
        _themePreferenceService = themePreferenceService;
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Window;

        _summaryLabel = new LabelControl
        {
            Dock = DockStyle.Top,
            Height = 30,
            Appearance = { Font = new Font("Segoe UI", 10, FontStyle.Bold) }
        };

        _currentSkinLabel = new LabelControl
        {
            Dock = DockStyle.Top,
            Height = 24
        };

        _currentPaletteLabel = new LabelControl
        {
            Dock = DockStyle.Top,
            Height = 24
        };

        _accentColor = new ColorPickEdit
        {
            Dock = DockStyle.Top,
            Height = 30,
            Properties =
            {
                AutomaticColor = Color.Empty,
                ShowSystemColors = true,
                ShowWebColors = true
            }
        };

        _previewTitle = new LabelControl
        {
            Dock = DockStyle.Top,
            Height = 34,
            Appearance = { Font = new Font("Segoe UI", 14, FontStyle.Bold) },
            Text = AppBranding.ApplicationName
        };

        _previewBody = new LabelControl
        {
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Height = 78,
            Text = "La selección de skin y paleta vive en la Ribbon contextual. Esta page solo muestra la vista previa, el acento y las opciones visuales globales."
        };

        _previewCard = new GroupControl
        {
            Dock = DockStyle.Fill,
            Text = "Vista previa"
        };
        _previewCard.Controls.Add(_previewBody);
        _previewCard.Controls.Add(_previewTitle);

        PanelControl optionsPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 104,
            BorderStyle = BorderStyles.NoBorder
        };
        optionsPanel.Controls.Add(_accentColor);
        optionsPanel.Controls.Add(_currentPaletteLabel);
        optionsPanel.Controls.Add(_currentSkinLabel);
        optionsPanel.Controls.Add(_summaryLabel);

        PanelControl previewHost = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyles.NoBorder,
            Padding = new Padding(16),
            MinimumSize = new Size(UiConstants.AppearancePreviewMinimumWidth, UiConstants.AppearancePreviewMinimumHeight)
        };
        previewHost.Controls.Add(_previewCard);
        previewHost.Controls.Add(optionsPanel);

        Controls.Add(previewHost);

        _skinGallery = new SkinRibbonGalleryBarItem
        {
            Caption = "Skins",
            RibbonStyle = RibbonItemStyles.Large,
            LargeWidth = 220,
            SmallWithTextWidth = 220,
            SmallWithoutTextWidth = 220
        };

        _paletteGallery = new SkinPaletteRibbonGalleryBarItem
        {
            Caption = "Paletas",
            RibbonStyle = RibbonItemStyles.Large,
            VisibleWithoutPalettes = false,
            LargeWidth = 220,
            SmallWithTextWidth = 220,
            SmallWithoutTextWidth = 220
        };

        _cmdSave = new BarButtonItem { Caption = "Aplicar y guardar" };
        _cmdReset = new BarButtonItem { Caption = "Restablecer" };
        _chkRoundedCorners = new BarCheckItem { Caption = "Esquinas redondeadas" };
        _chkCompactMode = new BarCheckItem { Caption = "Modo compacto" };

        _accentColor.EditValueChanged += (_, _) => ApplyAccentPreview();
        _cmdSave.ItemClick += (_, _) => SavePreference();
        _cmdReset.ItemClick += (_, _) => ResetTheme();
        _chkRoundedCorners.CheckedChanged += (_, _) => ApplyWindowPreviewFromRibbon();
        _chkCompactMode.CheckedChanged += (_, _) => ApplyWindowPreviewFromRibbon();

        // La galería cambia el look-and-feel global; refrescamos el preview al seleccionar.
        _skinGallery.GalleryItemClick += (_, _) => RefreshPreview();
        _paletteGallery.GalleryItemClick += (_, _) => RefreshPreview();

        LoadPreference();
        RefreshPreview();
        UserLookAndFeel.Default.StyleChanged += OnLookAndFeelChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UserLookAndFeel.Default.StyleChanged -= OnLookAndFeelChanged;
        }

        base.Dispose(disposing);
    }

    public void BuildRibbon(RibbonPage page)
    {
        page.Groups.Clear();

        // IMPORTANT: SkinHelper requiere que los items estén asociados al BarManager del ribbon.
        _skinGallery.Manager ??= page.Ribbon.Manager;
        _paletteGallery.Manager ??= page.Ribbon.Manager;
        _cmdSave.Manager ??= page.Ribbon.Manager;
        _cmdReset.Manager ??= page.Ribbon.Manager;
        _chkRoundedCorners.Manager ??= page.Ribbon.Manager;
        _chkCompactMode.Manager ??= page.Ribbon.Manager;

        AddItemIfNeeded(page.Ribbon, _skinGallery);
        AddItemIfNeeded(page.Ribbon, _paletteGallery);
        AddItemIfNeeded(page.Ribbon, _cmdSave);
        AddItemIfNeeded(page.Ribbon, _cmdReset);
        AddItemIfNeeded(page.Ribbon, _chkRoundedCorners);
        AddItemIfNeeded(page.Ribbon, _chkCompactMode);

        // Inicializar después de estar en el ribbon/manager para evitar NRE interno de DevExpress.
        SkinHelper.InitSkinGallery(_skinGallery);
        SkinHelper.InitSkinPaletteGallery(_paletteGallery);

        RibbonPageGroup skinGroup = new("Skins");
        skinGroup.ItemLinks.Add(_skinGallery);
        skinGroup.ItemLinks.Add(_paletteGallery);

        RibbonPageGroup optionsGroup = new("Opciones");
        optionsGroup.ItemLinks.Add(_cmdSave);
        optionsGroup.ItemLinks.Add(_cmdReset);
        optionsGroup.ItemLinks.Add(_chkRoundedCorners);
        optionsGroup.ItemLinks.Add(_chkCompactMode);

        page.Groups.Add(skinGroup);
        page.Groups.Add(optionsGroup);
    }

    public void OnActivated()
    {
        RefreshPreview();
    }

    private void LoadPreference()
    {
        ThemePreference pref = _themePreferenceService.Load();

        if (!string.IsNullOrWhiteSpace(pref.AccentColorHex))
        {
            _accentColor.EditValue = ColorTranslator.FromHtml(pref.AccentColorHex);
        }

        _chkRoundedCorners.Checked = pref.RoundedWindowCorners;
        _chkCompactMode.Checked = pref.CompactUIMode;
        ApplyAccentPreview();
        ApplyWindowPreviewFromRibbon();
    }

    private void ApplyAccentPreview()
    {
        if (_accentColor.EditValue is Color color && color != Color.Empty)
        {
            UserLookAndFeel.Default.SkinMaskColor = color;
            UserLookAndFeel.Default.SkinMaskColor2 = color;
        }
        else
        {
            UserLookAndFeel.Default.ResetSkinMaskColors();
        }

        RefreshPreview();
    }

    private void ApplyWindowPreviewFromRibbon()
    {
        WindowsFormsSettings.AllowRoundedWindowCorners = _chkRoundedCorners.Checked
            ? DevExpress.Utils.DefaultBoolean.True
            : DevExpress.Utils.DefaultBoolean.False;

        UserLookAndFeel.Default.CompactUIMode = _chkCompactMode.Checked
            ? DevExpress.Utils.DefaultBoolean.True
            : DevExpress.Utils.DefaultBoolean.False;

        RefreshPreview();
    }

    private void SavePreference()
    {
        ThemePreference preference = new(
            SkinName: UserLookAndFeel.Default.ActiveSkinName,
            PaletteName: UserLookAndFeel.Default.ActiveSvgPaletteName,
            AccentColorHex: _accentColor.EditValue is Color color && color != Color.Empty ? ColorTranslator.ToHtml(color) : null,
            RoundedWindowCorners: _chkRoundedCorners.Checked,
            CompactUIMode: _chkCompactMode.Checked);

        _themePreferenceService.Save(preference);
        XtraMessageBox.Show(this, "Preferencia de apariencia guardada.", "SecureERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ResetTheme()
    {
        ThemePreference defaultPreference = ThemePreference.Default;
        UserLookAndFeel.Default.SetSkinStyle(defaultPreference.SkinName);
        _themePreferenceService.Save(defaultPreference);
        _accentColor.EditValue = null;
        _chkRoundedCorners.Checked = defaultPreference.RoundedWindowCorners;
        _chkCompactMode.Checked = defaultPreference.CompactUIMode;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        string skin = string.IsNullOrWhiteSpace(UserLookAndFeel.Default.ActiveSkinName)
            ? UiConstants.DefaultSkin
            : UserLookAndFeel.Default.ActiveSkinName;

        string? palette = UserLookAndFeel.Default.ActiveSvgPaletteName;
        string mode = _chkCompactMode.Checked ? "Compacto" : "Estándar";
        string corners = _chkRoundedCorners.Checked ? "Redondeadas" : "Rectas";

        _summaryLabel.Text = "Vista de apariencia ERP";
        _currentSkinLabel.Text = $"Skin: {skin}";
        _currentPaletteLabel.Text = string.IsNullOrWhiteSpace(palette) ? "Paleta: (sin paleta SVG)" : $"Paleta: {palette}";
        _previewTitle.Text = $"{AppBranding.ApplicationName} - {skin}";
        _previewBody.Text = $"Paleta actual: {palette ?? "predeterminada"} · Modo: {mode} · Esquinas: {corners}.";
    }

    private void OnLookAndFeelChanged(object? sender, EventArgs e)
    {
        RefreshPreview();
    }

    private static void AddItemIfNeeded(RibbonControl ribbon, BarItem item)
    {
        if (!ribbon.Items.Contains(item))
        {
            ribbon.Items.Add(item);
        }
    }
}
