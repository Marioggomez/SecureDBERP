using DevExpress.XtraEditors;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Shell.Shell;

public sealed class LoginForm : XtraForm
{
    private readonly IAuthenticationService _authenticationService;
    private readonly TextEdit _userText;
    private readonly TextEdit _passwordText;
    private readonly TextEdit _tenantText;
    private readonly SimpleButton _loginButton;

    public LoginForm(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        Text = "SecureERP - Iniciar sesion";
        Width = 420;
        Height = 300;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var layout = new DevExpress.XtraLayout.LayoutControl
        {
            Dock = DockStyle.Fill
        };

        _userText = new TextEdit();
        _passwordText = new TextEdit { Properties = { UseSystemPasswordChar = true } };
        _tenantText = new TextEdit();

        _loginButton = new SimpleButton
        {
            Text = "Entrar"
        };
        _loginButton.Click += async (_, _) => await LoginAsync();

        var cancelButton = new SimpleButton
        {
            Text = "Salir"
        };
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        layout.Controls.Add(_userText);
        layout.Controls.Add(_passwordText);
        layout.Controls.Add(_tenantText);
        layout.Controls.Add(_loginButton);
        layout.Controls.Add(cancelButton);

        var root = new DevExpress.XtraLayout.LayoutControlGroup();
        layout.Root = root;
        root.AddItem("Usuario", _userText);
        root.AddItem("Contrasena", _passwordText);
        root.AddItem("Empresa/Tenant", _tenantText);
        root.AddItem(string.Empty, _loginButton);
        root.AddItem(string.Empty, cancelButton);

        Controls.Add(layout);
        AcceptButton = _loginButton;
    }

    private async Task LoginAsync()
    {
        _loginButton.Enabled = false;

        try
        {
            var request = new LoginRequest(_userText.Text.Trim(), _passwordText.Text, _tenantText.Text.Trim());
            await _authenticationService.SignInAsync(request);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, ex.Message, "Login SecureERP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _loginButton.Enabled = true;
        }
    }
}
