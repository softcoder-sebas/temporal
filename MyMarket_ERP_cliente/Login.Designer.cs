using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Login
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelLeft = new Panel();
            lblLeftTagline = new Label();
            lblLeftTitle = new Label();
            panelLeftAccent = new Panel();
            panelRight = new Panel();
            tableRight = new TableLayoutPanel();
            cardLogin = new Panel();
            tableCard = new TableLayoutPanel();
            lblWelcome = new Label();
            lblDescription = new Label();
            lblEmail = new Label();
            panelEmail = new Panel();
            txtEmail = new TextBox();
            lblPassword = new Label();
            panelPassword = new Panel();
            btnTogglePassword = new Button();
            txtPassword = new TextBox();
            cardSpacer = new Panel();
            btnLogin = new Button();
            panelLeft.SuspendLayout();
            panelRight.SuspendLayout();
            tableRight.SuspendLayout();
            cardLogin.SuspendLayout();
            tableCard.SuspendLayout();
            panelEmail.SuspendLayout();
            panelPassword.SuspendLayout();
            SuspendLayout();
            // 
            // panelLeft
            // 
            panelLeft.BackColor = Color.FromArgb(99, 102, 241);
            panelLeft.Controls.Add(lblLeftTagline);
            panelLeft.Controls.Add(lblLeftTitle);
            panelLeft.Controls.Add(panelLeftAccent);
            panelLeft.Dock = DockStyle.Left;
            panelLeft.Location = new Point(0, 0);
            panelLeft.Name = "panelLeft";
            panelLeft.Padding = new Padding(40, 60, 40, 40);
            panelLeft.Size = new Size(443, 760);
            panelLeft.TabIndex = 0;
            // 
            // lblLeftTagline
            // 
            lblLeftTagline.AutoSize = true;
            lblLeftTagline.Font = new Font("Segoe UI", 10F);
            lblLeftTagline.ForeColor = Color.FromArgb(226, 236, 255);
            lblLeftTagline.Location = new Point(40, 177);
            lblLeftTagline.MaximumSize = new Size(240, 0);
            lblLeftTagline.Name = "lblLeftTagline";
            lblLeftTagline.Size = new Size(236, 69);
            lblLeftTagline.TabIndex = 2;
            lblLeftTagline.Text = "Gestiona inventarios, ventas y reportes en una sola plataforma ágil.";
            lblLeftTagline.Click += lblLeftTagline_Click;
            // 
            // lblLeftTitle
            // 
            lblLeftTitle.AutoSize = true;
            lblLeftTitle.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            lblLeftTitle.ForeColor = Color.White;
            lblLeftTitle.Location = new Point(40, 100);
            lblLeftTitle.Name = "lblLeftTitle";
            lblLeftTitle.Size = new Size(352, 62);
            lblLeftTitle.TabIndex = 1;
            lblLeftTitle.Text = "MyMarket ERP";
            // 
            // panelLeftAccent
            // 
            panelLeftAccent.BackColor = Color.FromArgb(59, 130, 246);
            panelLeftAccent.Dock = DockStyle.Top;
            panelLeftAccent.Location = new Point(40, 60);
            panelLeftAccent.Name = "panelLeftAccent";
            panelLeftAccent.Size = new Size(363, 3);
            panelLeftAccent.TabIndex = 0;
            // 
            // panelRight
            // 
            panelRight.BackColor = Color.FromArgb(248, 250, 252);
            panelRight.Controls.Add(tableRight);
            panelRight.Dock = DockStyle.Fill;
            panelRight.Location = new Point(443, 0);
            panelRight.Name = "panelRight";
            panelRight.Padding = new Padding(40);
            panelRight.Size = new Size(698, 760);
            panelRight.TabIndex = 1;
            // 
            // tableRight
            // 
            tableRight.ColumnCount = 3;
            tableRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
            tableRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableRight.Controls.Add(cardLogin, 1, 1);
            tableRight.Dock = DockStyle.Fill;
            tableRight.Location = new Point(40, 40);
            tableRight.Name = "tableRight";
            tableRight.RowCount = 3;
            tableRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 520F));
            tableRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableRight.Size = new Size(618, 680);
            tableRight.TabIndex = 0;
            // 
            // cardLogin
            // 
            cardLogin.BackColor = Color.Transparent;
            cardLogin.Controls.Add(tableCard);
            cardLogin.Location = new Point(102, 83);
            cardLogin.Name = "cardLogin";
            cardLogin.Padding = new Padding(36);
            cardLogin.Size = new Size(414, 514);
            cardLogin.TabIndex = 0;
            cardLogin.Paint += CardLogin_Paint;
            // 
            // tableCard
            // 
            tableCard.BackColor = Color.Transparent;
            tableCard.ColumnCount = 1;
            tableCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableCard.Controls.Add(lblWelcome, 0, 0);
            tableCard.Controls.Add(lblDescription, 0, 1);
            tableCard.Controls.Add(lblEmail, 0, 2);
            tableCard.Controls.Add(panelEmail, 0, 3);
            tableCard.Controls.Add(lblPassword, 0, 4);
            tableCard.Controls.Add(panelPassword, 0, 5);
            tableCard.Controls.Add(cardSpacer, 0, 6);
            tableCard.Controls.Add(btnLogin, 0, 7);
            tableCard.Dock = DockStyle.Fill;
            tableCard.Location = new Point(36, 36);
            tableCard.Margin = new Padding(0);
            tableCard.Name = "tableCard";
            tableCard.RowCount = 8;
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableCard.RowStyles.Add(new RowStyle());
            tableCard.Size = new Size(342, 442);
            tableCard.TabIndex = 0;
            // 
            // lblWelcome
            // 
            lblWelcome.AutoSize = true;
            lblWelcome.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblWelcome.ForeColor = Color.FromArgb(15, 23, 42);
            lblWelcome.Location = new Point(0, 0);
            lblWelcome.Margin = new Padding(0, 0, 0, 8);
            lblWelcome.MaximumSize = new Size(320, 0);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(281, 100);
            lblWelcome.TabIndex = 0;
            lblWelcome.Text = "Bienvenido de vuelta";
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Font = new Font("Segoe UI", 10F);
            lblDescription.ForeColor = Color.FromArgb(100, 116, 139);
            lblDescription.Location = new Point(0, 108);
            lblDescription.Margin = new Padding(0, 0, 0, 24);
            lblDescription.MaximumSize = new Size(320, 0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(318, 46);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "Ingresa tus credenciales para acceder al panel de MyMarket.";
            lblDescription.Click += lblDescription_Click;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 10F);
            lblEmail.ForeColor = Color.FromArgb(100, 116, 139);
            lblEmail.Location = new Point(0, 178);
            lblEmail.Margin = new Padding(0, 0, 0, 6);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(151, 23);
            lblEmail.TabIndex = 2;
            lblEmail.Text = "Correo electrónico";
            // 
            // panelEmail
            // 
            panelEmail.BackColor = Color.FromArgb(248, 250, 252);
            panelEmail.Controls.Add(txtEmail);
            panelEmail.Dock = DockStyle.Fill;
            panelEmail.Location = new Point(0, 207);
            panelEmail.Margin = new Padding(0, 0, 0, 16);
            panelEmail.Name = "panelEmail";
            panelEmail.Padding = new Padding(12, 8, 12, 8);
            panelEmail.Size = new Size(342, 55);
            panelEmail.TabIndex = 7;
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.FromArgb(248, 250, 252);
            txtEmail.BorderStyle = BorderStyle.None;
            txtEmail.Dock = DockStyle.Fill;
            txtEmail.Font = new Font("Segoe UI", 10F);
            txtEmail.ForeColor = Color.FromArgb(15, 23, 42);
            txtEmail.Location = new Point(12, 8);
            txtEmail.MaxLength = 100;
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "nombre@empresa.com";
            txtEmail.Size = new Size(318, 23);
            txtEmail.TabIndex = 0;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 10F);
            lblPassword.ForeColor = Color.FromArgb(100, 116, 139);
            lblPassword.Location = new Point(0, 278);
            lblPassword.Margin = new Padding(0, 0, 0, 6);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(97, 23);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Contraseña";
            // 
            // panelPassword
            // 
            panelPassword.BackColor = Color.FromArgb(248, 250, 252);
            panelPassword.Controls.Add(btnTogglePassword);
            panelPassword.Controls.Add(txtPassword);
            panelPassword.Dock = DockStyle.Fill;
            panelPassword.Location = new Point(0, 307);
            panelPassword.Margin = new Padding(0, 0, 0, 16);
            panelPassword.Name = "panelPassword";
            panelPassword.Padding = new Padding(12, 8, 12, 8);
            panelPassword.Size = new Size(342, 55);
            panelPassword.TabIndex = 5;
            panelPassword.Paint += panelPassword_Paint;
            // 
            // btnTogglePassword
            // 
            btnTogglePassword.BackColor = Color.Transparent;
            btnTogglePassword.Dock = DockStyle.Right;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.Font = new Font("Segoe UI Emoji", 12F);
            btnTogglePassword.ForeColor = Color.FromArgb(100, 116, 139);
            btnTogglePassword.Location = new Point(290, 8);
            btnTogglePassword.Name = "btnTogglePassword";
            btnTogglePassword.Size = new Size(40, 39);
            btnTogglePassword.TabIndex = 2;
            btnTogglePassword.TabStop = false;
            btnTogglePassword.Text = "👁";
            btnTogglePassword.UseVisualStyleBackColor = false;
            btnTogglePassword.Click += BtnTogglePassword_Click;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.FromArgb(248, 250, 252);
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.ForeColor = Color.FromArgb(15, 23, 42);
            txtPassword.Location = new Point(12, 8);
            txtPassword.MaxLength = 64;
            txtPassword.Name = "txtPassword";
            txtPassword.PlaceholderText = "Ingresa tu contraseña";
            txtPassword.Size = new Size(318, 23);
            txtPassword.TabIndex = 1;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // cardSpacer
            // 
            cardSpacer.Dock = DockStyle.Fill;
            cardSpacer.Location = new Point(0, 378);
            cardSpacer.Margin = new Padding(0);
            cardSpacer.Name = "cardSpacer";
            cardSpacer.Size = new Size(342, 1);
            cardSpacer.TabIndex = 8;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(99, 102, 241);
            btnLogin.Dock = DockStyle.Fill;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(0, 398);
            btnLogin.Margin = new Padding(0, 24, 0, 0);
            btnLogin.MinimumSize = new Size(0, 44);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(342, 44);
            btnLogin.TabIndex = 3;
            btnLogin.Text = "Ingresar";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += BtnLogin_Click;
            // 
            // Login
            // 
            AcceptButton = btnLogin;
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 250, 252);
            ClientSize = new Size(1141, 760);
            Controls.Add(panelRight);
            Controls.Add(panelLeft);
            Font = new Font("Segoe UI", 10F);
            ForeColor = Color.FromArgb(15, 23, 42);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Login";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MyMarket ERP - Acceso";
            panelLeft.ResumeLayout(false);
            panelLeft.PerformLayout();
            panelRight.ResumeLayout(false);
            tableRight.ResumeLayout(false);
            cardLogin.ResumeLayout(false);
            tableCard.ResumeLayout(false);
            tableCard.PerformLayout();
            panelEmail.ResumeLayout(false);
            panelEmail.PerformLayout();
            panelPassword.ResumeLayout(false);
            panelPassword.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelLeft;
        private Panel panelRight;
        private Panel panelLeftAccent;
        private Label lblLeftTitle;
        private Label lblLeftTagline;
        private TableLayoutPanel tableRight;
        private Panel cardLogin;
        private TableLayoutPanel tableCard;
        private Label lblWelcome;
        private Label lblDescription;
        private Label lblEmail;
        private Panel panelEmail;
        private TextBox txtEmail;
        private Label lblPassword;
        private Panel panelPassword;
        private Button btnTogglePassword;
        private TextBox txtPassword;
        private Button btnLogin;
        private Panel cardSpacer;
    }
}
