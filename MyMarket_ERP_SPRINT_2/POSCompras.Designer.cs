using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class POSCompras
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblTitulo;

        private TextBox txtScan;
        private NumericUpDown numQty;
        private Button btnAgregar;
        private Button btnBuscar;
        private Button btnEliminarSel;

        private DataGridView gridCart;

        private CheckBox chkIVA;
        private Label lblSubtotal;
        private Label lblIVA;
        private Label lblTotal;

        private ComboBox cboPago;
        private TextBox txtCliente;

        private Button btnCobrar;
        private Button btnCancelar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = ModernTheme.Background;
            ClientSize = new Size(1200, 720);
            Font = ModernTheme.Body;
            ForeColor = ModernTheme.TextPrimary;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "POS - Punto de Venta";

            layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(24),
                BackColor = Color.Transparent
            };
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            BuildTopBar();
            BuildContent();

            layoutRoot.Controls.Add(topBar, 0, 0);
            layoutRoot.Controls.Add(BuildMainContent(), 0, 1);

            Controls.Add(layoutRoot);
        }

        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(0, 0, 0, 8)
            };

            lblTitulo = new Label
            {
                Text = "Ventas",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            topBar.Controls.Add(lblTitulo);
        }

        private Panel BuildMainContent()
        {
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var mainCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(24)
            };
            mainCard.Paint += (s, e) => DrawCard(e.Graphics, mainCard.ClientRectangle);

            // Panel superior: Escaneo y búsqueda
            var scanPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(0, 0, 0, 12)
            };

            var lblScan = new Label
            {
                Text = "Código / Nombre:",
                AutoSize = true,
                Location = new Point(0, 6),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary
            };

            txtScan = new TextBox
            {
                Location = new Point(130, 3),
                Width = 400,
                Font = new Font("Segoe UI", 11f)
            };

            var lblQty = new Label
            {
                Text = "Cantidad:",
                AutoSize = true,
                Location = new Point(545, 6),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary
            };

            numQty = new NumericUpDown
            {
                Location = new Point(615, 3),
                Width = 80,
                Minimum = 1,
                Maximum = 999,
                Value = 1,
                Font = new Font("Segoe UI", 11f)
            };

            btnAgregar = ModernTheme.CreatePrimaryButton("Agregar (Enter)");
            btnAgregar.Location = new Point(710, 2);
            btnAgregar.Width = 140;

            btnBuscar = ModernTheme.CreateGhostButton("🔍 Buscar (F2)");
            btnBuscar.Location = new Point(860, 2);
            btnBuscar.Width = 140;

            scanPanel.Controls.AddRange(new Control[] {
                lblScan, txtScan, lblQty, numQty, btnAgregar, btnBuscar
            });

            // Grid del carrito
            gridCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BorderStyle = BorderStyle.None
            };
            ModernTheme.StyleDataGrid(gridCart);

            // Panel inferior: Totales y acciones
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                Padding = new Padding(0, 12, 0, 0)
            };

            btnEliminarSel = ModernTheme.CreateGhostButton("🗑️ Eliminar línea");
            btnEliminarSel.Location = new Point(0, 12);
            btnEliminarSel.Width = 150;

            chkIVA = new CheckBox
            {
                Text = "Aplicar IVA (19%)",
                Location = new Point(170, 16),
                Checked = true,
                Font = new Font("Segoe UI", 9f)
            };

            // Totales (derecha)
            var totalsPanel = new Panel
            {
                Width = 300,
                Height = 120,
                Location = new Point(700, 0)
            };

            var lblSubtotalCaption = new Label
            {
                Text = "Subtotal:",
                Location = new Point(0, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f)
            };
            lblSubtotal = new Label
            {
                Text = "$0.00",
                Location = new Point(120, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            var lblIVACaption = new Label
            {
                Text = "IVA (19%):",
                Location = new Point(0, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f)
            };
            lblIVA = new Label
            {
                Text = "$0.00",
                Location = new Point(120, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            var lblTotalCaption = new Label
            {
                Text = "TOTAL:",
                Location = new Point(0, 65),
                AutoSize = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ModernTheme.Accent
            };
            lblTotal = new Label
            {
                Text = "$0.00",
                Location = new Point(120, 65),
                AutoSize = true,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ModernTheme.Accent
            };

            totalsPanel.Controls.AddRange(new Control[] {
                lblSubtotalCaption, lblSubtotal,
                lblIVACaption, lblIVA,
                lblTotalCaption, lblTotal
            });

            // Pago y cliente
            var lblPago = new Label
            {
                Text = "Método pago:",
                Location = new Point(0, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = ModernTheme.TextSecondary
            };

            cboPago = new ComboBox
            {
                Location = new Point(100, 56),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblCli = new Label
            {
                Text = "Cliente:",
                Location = new Point(300, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = ModernTheme.TextSecondary
            };

            txtCliente = new TextBox
            {
                Location = new Point(360, 56),
                Width = 300,
                PlaceholderText = "Nombre del cliente (opcional)"
            };

            // Botones de acción
            btnCancelar = ModernTheme.CreateGhostButton("✕ Cancelar venta");
            btnCancelar.Location = new Point(0, 130);
            btnCancelar.Width = 180;
            btnCancelar.Height = 40;

            btnCobrar = ModernTheme.CreatePrimaryButton("💰 Cobrar");
            btnCobrar.Location = new Point(820, 130);
            btnCobrar.Width = 180;
            btnCobrar.Height = 40;

            bottomPanel.Controls.AddRange(new Control[] {
                btnEliminarSel, chkIVA, totalsPanel,
                lblPago, cboPago, lblCli, txtCliente,
                btnCancelar, btnCobrar
            });

            mainCard.Controls.Add(gridCart);
            mainCard.Controls.Add(bottomPanel);
            mainCard.Controls.Add(scanPanel);

            contentPanel.Controls.Add(mainCard);

            return contentPanel;
        }

        private void BuildContent()
        {
            // Este método se mantiene para compatibilidad pero el contenido
            // se construye en BuildMainContent()
        }

        private static void DrawCard(Graphics g, Rectangle bounds)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 18;
            int diameter = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            using var brush = new SolidBrush(ModernTheme.Surface);
            using var border = new Pen(ModernTheme.Border);
            g.FillPath(brush, path);
            g.DrawPath(border, path);
        }
    }
}