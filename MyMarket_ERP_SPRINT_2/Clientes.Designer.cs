using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Clientes
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblTitulo;
        private TextBox txtBuscar;
        private Label lblBuscar;
        private Button btnNuevo;
        private Button btnEditar;
        private Button btnEliminar;

        private TableLayoutPanel contentLayout;
        private Panel gridPanel;
        private DataGridView gridClientes;
        private Panel actionBar;
        private FlowLayoutPanel actionButtons;
        private Label lblStatus;
        private Label lblSegmentDescription;

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
            ClientSize = new Size(1360, 820);
            Font = ModernTheme.Body;
            ForeColor = ModernTheme.TextPrimary;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Clientes";

            layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.Transparent
            };
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // topBar
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // actionBar
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // content

            BuildTopBar();
            BuildActionBar();
            BuildContent();

            layoutRoot.Controls.Add(topBar, 0, 0);
            layoutRoot.Controls.Add(actionBar, 0, 1);
            layoutRoot.Controls.Add(contentLayout, 0, 2);

            Controls.Add(layoutRoot);
        }

        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 16)
            };

            var headerLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblTitulo = new Label
            {
                Text = "Clientes",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var filterBar = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            lblBuscar = new Label
            {
                Text = "Buscar",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 8, 12, 8)
            };

            txtBuscar = new TextBox
            {
                PlaceholderText = "Nombre, correo o documento…",
                Width = 320,
                Margin = new Padding(0, 4, 12, 8),
                TabIndex = 0
            };
            txtBuscar.AccessibleName = "Buscar clientes";

            filterBar.Controls.Add(lblBuscar);
            filterBar.Controls.Add(txtBuscar);

            headerLayout.Controls.Add(lblTitulo, 0, 0);
            headerLayout.Controls.Add(filterBar, 0, 1);

            topBar.Controls.Add(headerLayout);
        }

        private void BuildActionBar()
        {
            actionBar = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, 16)
            };

            var actionLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblStatus = new Label
            {
                Text = "Clientes: 0",
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(0, 6, 0, 0)
            };

            actionButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            btnNuevo = ModernTheme.CreatePrimaryButton("＋ Nuevo cliente");
            btnNuevo.Margin = new Padding(8, 0, 0, 0);

            btnEditar = ModernTheme.CreateGhostButton("Editar seleccionado");
            btnEditar.Margin = new Padding(8, 0, 0, 0);

            btnEliminar = ModernTheme.CreateGhostButton("Eliminar seleccionado");
            btnEliminar.Margin = new Padding(8, 0, 0, 0);
            btnEliminar.Width = 170;

            actionButtons.Controls.Add(btnEliminar);
            actionButtons.Controls.Add(btnEditar);
            actionButtons.Controls.Add(btnNuevo);

            actionLayout.Controls.Add(lblStatus, 0, 0);
            actionLayout.Controls.Add(actionButtons, 1, 0);

            lblSegmentDescription = new Label
            {
                Text = "Segmento A (máximo): objetivo ≥ 10 compras • total ≥ $1.200.000 • ticket ≥ $120.000 • score ≥ 70%\nSegmento B (intermedio): objetivo ≥ 5 compras • total ≥ $400.000 • ticket ≥ $80.000 • score ≥ 40%\nSegmento C (mínimo): clientes con baja frecuencia o gasto acumulado.",
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(0, 4, 0, 0),
                MaximumSize = new Size(1000, 0)
            };

            actionLayout.Controls.Add(lblSegmentDescription, 0, 1);
            actionLayout.SetColumnSpan(lblSegmentDescription, 2);

            actionBar.Controls.Add(actionLayout);
        }

        private void BuildContent()
        {
            contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            BuildGridPanel();
            contentLayout.Controls.Add(gridPanel, 0, 0);
        }

        private void BuildGridPanel()
        {
            gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(16)
            };
            card.Paint += (s, e) => DrawCard(e.Graphics, card.ClientRectangle);

            gridClientes = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                TabIndex = 5
            };
            gridClientes.AccessibleName = "Listado de clientes";

            card.Controls.Add(gridClientes);
            gridPanel.Controls.Add(card);
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Padding = new Padding(0, 6, 16, 6)
            };
        }

        private static TextBox CreateTextBox(int width)
        {
            return new TextBox
            {
                Width = width,
                Dock = DockStyle.Fill
            };
        }

        private static void DrawCard(Graphics g, Rectangle bounds)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 16;
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