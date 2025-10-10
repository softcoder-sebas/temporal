using System;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class Login : Form
    {
        // Estado del botón ojo
        private bool _showingPassword = false;

        public Login()
        {
            InitializeComponent();

            // === Config de controles existentes ===
            txtEmail.MaxLength = 100; // email
            txtPassword.MaxLength = 64;  // password
            txtPassword.UseSystemPasswordChar = true;   // inicia oculto
            this.AcceptButton = btnLogin;               // Enter = Ingresar
            btnLogin.Visible = true;
            btnLogin.Enabled = true;
            btnLogin.BringToFront();
            btnTogglePassword.Text = "👁";               // icono inicial

            // Evitar espacios en email y password
            txtEmail.KeyPress += (s, e) => { if (char.IsWhiteSpace(e.KeyChar)) e.Handled = true; };
            txtPassword.KeyPress += (s, e) => { if (char.IsWhiteSpace(e.KeyChar)) e.Handled = true; };

            Load += Login_Load;

            // Inicializa la base de datos y siembra usuarios de ejemplo
            Database.EnsureInitialized();
        }

        private void Login_Load(object? sender, EventArgs e)
        {
            PrepareLoginUi();
        }

        private void PrepareLoginUi()
        {
            if (!IsHandleCreated)
                return;

            btnLogin.Visible = true;
            btnLogin.Enabled = true;
            btnLogin.BringToFront();

            BeginInvoke(new Action(() =>
            {
                Control target = string.IsNullOrWhiteSpace(txtEmail.Text) ? txtEmail : txtPassword;
                if (target == txtPassword)
                {
                    txtPassword.SelectAll();
                }
                else
                {
                    txtEmail.SelectAll();
                }
                target.Focus();
            }));
        }

        // Validaciones de entrada
        private bool ValidateInputs(out string msg)
        {
            var email = txtEmail.Text.Trim();
            var pass = txtPassword.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                msg = "Escribe correo y contraseña.";
                return false;
            }
            if (email.Length > 100)
            {
                msg = "El correo no puede superar 100 caracteres.";
                return false;
            }
            if (pass.Length > 64)
            {
                msg = "La contraseña no puede superar 64 caracteres.";
                return false;
            }
            var rx = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!rx.IsMatch(email))
            {
                msg = "Formato de correo inválido.";
                return false;
            }
            msg = "";
            return true;
        }

        // Iniciar sesión
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs(out var whyNot))
                {
                    MessageBox.Show(whyNot, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var email = txtEmail.Text.Trim();
                var pass = txtPassword.Text;

                using var cn = Database.OpenConnection();
                string? storedHash = null;
                string? role = null;
                int? customerId = null;

                using (var cmd = new SqlCommand(@"SELECT TOP (1) Password, Role, CustomerId
                            FROM dbo.Users WHERE Email=@e AND IsActive=1", cn))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    using var rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        storedHash = rd.IsDBNull(0) ? null : rd.GetString(0);
                        role = rd.IsDBNull(1) ? null : rd.GetString(1);
                        if (!rd.IsDBNull(2))
                            customerId = rd.GetInt32(2);
                    }
                }

                if (storedHash == null || role == null)
                {
                    MessageBox.Show("Credenciales inválidas o usuario inactivo.",
                        "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                string hashInput = PasswordHasher.Hash(pass);
                if (!string.Equals(storedHash, hashInput, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Credenciales inválidas o usuario inactivo.",
                        "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                if (string.Equals(role, "cliente", StringComparison.OrdinalIgnoreCase) && customerId == null)
                {
                    using var lookup = new SqlCommand(@"SELECT TOP (1) Id FROM dbo.Customers WHERE Email=@e", cn);
                    lookup.Parameters.AddWithValue("@e", email);
                    var result = lookup.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        customerId = Convert.ToInt32(result);
                        using var update = new SqlCommand("UPDATE dbo.Users SET CustomerId=@cid WHERE Email=@e;", cn);
                        update.Parameters.AddWithValue("@cid", customerId.Value);
                        update.Parameters.AddWithValue("@e", email);
                        update.ExecuteNonQuery();
                    }
                }

                // Guardar sesión
                AppSession.StartSession(email, role, customerId);

                Form nextForm;
                if (string.Equals(role, "cliente", StringComparison.OrdinalIgnoreCase))
                {
                    nextForm = new Historial_facturacion
                    {
                        Tag = NavSection.Historial
                    };
                }
                else
                {
                    nextForm = new Central(role)
                    {
                        Tag = NavSection.Central
                    };
                }

                nextForm.FormClosed += MainForm_FormClosed;

                Hide();
                nextForm.Show();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Error de base de datos:\n" + ex.Message, "SQL Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string fullMessage =
                    "Ocurrió un error inesperado:\n\n" +
                    ex.Message +
                    "\n\n--- Detalles ---\n" +
                    ex.ToString();

                MessageBox.Show(
                    fullMessage,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is Form form)
                form.FormClosed -= MainForm_FormClosed;

            if (AppSession.HasActiveSession)
            {
                Application.Exit();
                return;
            }

            txtPassword.Clear();
            Show();
            Activate();
            PrepareLoginUi();
        }

        // Ojo: ver/ocultar contraseña
        private void BtnTogglePassword_Click(object sender, EventArgs e)
        {
            int caret = txtPassword.SelectionStart; // mantiene el cursor
            _showingPassword = !_showingPassword;
            txtPassword.UseSystemPasswordChar = !_showingPassword;
            btnTogglePassword.Text = _showingPassword ? "🙈" : "👁";
            txtPassword.SelectionStart = caret;
            txtPassword.Focus();
        }

        private void CardLogin_Paint(object sender, PaintEventArgs e)
        {
            var rect = cardLogin.ClientRectangle;
            rect = Rectangle.Inflate(rect, -1, -1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var shadowRect = Rectangle.Inflate(rect, -8, -8);
            shadowRect.Offset(6, 10);
            if (shadowRect.Width <= 0 || shadowRect.Height <= 0)
                return;

            using var shadowPath = CreateRoundedRect(shadowRect, 18);
            using var shadow = new PathGradientBrush(shadowPath)
            {
                CenterColor = Color.FromArgb(45, Color.Black),
                SurroundColors = new[] { Color.FromArgb(0, Color.Black) }
            };
            var cardRect = Rectangle.Inflate(rect, -6, -6);
            if (cardRect.Width <= 0 || cardRect.Height <= 0)
                return;

            using var surface = new SolidBrush(ModernTheme.Surface);
            using var border = new Pen(ModernTheme.Border);
            using var cardPath = CreateRoundedRect(cardRect, 18);

            e.Graphics.FillPath(shadow, shadowPath);
            e.Graphics.FillPath(surface, cardPath);
            e.Graphics.DrawPath(border, cardPath);
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void lblDescription_Click(object sender, EventArgs e)
        {

        }

        private void lblLeftTagline_Click(object sender, EventArgs e)
        {

        }

        private void chkRemember_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void linkForgot_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void panelPassword_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}