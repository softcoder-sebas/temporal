using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Inventario
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblTitulo;
        private Label lblSub;

        private TabControl tabControl;
        private TabPage tabOrdenes;
        private TabPage tabProductos;

        // === TAB ÓRDENES ===
        private Button btnNuevaCompra;
        private Button btnRegistrarProveedor;
        private Button btnEvaluarProveedor;
        private TextBox txtBuscar;
        private ComboBox cmbEstado;
        private CheckBox chkFecha;
        private DateTimePicker dtDesde;
        private DateTimePicker dtHasta;
        private DataGridView grid;
        private Label lblRango;
        private Button btnAnterior;
        private Button btnSiguiente;
        private Button btnToggleDetalle;
        private Panel pnlDetalle;
        private Label lblOrd;
        private Label lblProv;
        private Label lblFecha;
        private Label lblEstado;
        private ListView listProductos;
        private Label lblTotal;
        private DataGridView gridProveedores;
        private Label lblRankingTitulo;
        private Label lblRankingResumen;

        // === TAB PRODUCTOS ===
        private TextBox txtBuscarProducto;
        private ComboBox cmbCategoriaProducto;
        private CheckBox chkSoloCritico;
        private Button btnNuevoProducto;
        private Button btnEditarProducto;
        private Button btnEliminarProducto;
        private Button btnRefrescarProductos;
        private DataGridView gridProductos;
        private Label lblConteoProductos;

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
            ClientSize = new Size(1400, 860);
            Font = ModernTheme.Body;
            ForeColor = ModernTheme.TextPrimary;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Inventario";

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
            BuildTabs();

            layoutRoot.Controls.Add(topBar, 0, 0);
            layoutRoot.Controls.Add(tabControl, 0, 1);

            Controls.Add(layoutRoot);
        }

        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(0, 0, 0, 16)
            };

            lblTitulo = new Label
            {
                Text = "Inventario",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            lblSub = new Label
            {
                Text = "Gestiona órdenes, proveedores y productos",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 36)
            };

            topBar.Controls.Add(lblTitulo);
            topBar.Controls.Add(lblSub);
        }

        private void BuildTabs()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f)
            };

            tabOrdenes = new TabPage("Órdenes y Proveedores");
            tabProductos = new TabPage("Productos");

            BuildTabOrdenes();
            BuildTabProductos();

            tabControl.TabPages.Add(tabOrdenes);
            tabControl.TabPages.Add(tabProductos);
        }

        private void BuildTabOrdenes()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };

            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(0, 0, 0, 16)
            };

            btnNuevaCompra = ModernTheme.CreatePrimaryButton("📋 Nueva cotización");
            btnNuevaCompra.AccessibleName = "Crear nueva cotización";
            btnNuevaCompra.Location = new Point(0, 6);
            btnNuevaCompra.Width = 170;

            btnRegistrarProveedor = ModernTheme.CreateSecondaryButton("Registrar proveedor");
            btnRegistrarProveedor.Location = new Point(182, 6);
            btnRegistrarProveedor.Width = 160;

            btnEvaluarProveedor = ModernTheme.CreateSecondaryButton("Evaluar proveedor");
            btnEvaluarProveedor.Location = new Point(352, 6);
            btnEvaluarProveedor.Width = 160;

            txtBuscar = new TextBox
            {
                PlaceholderText = "Buscar por número o proveedor…",
                Width = 260,
                Location = new Point(522, 10)
            };

            cmbEstado = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Location = new Point(794, 10)
            };

            chkFecha = new CheckBox
            {
                Text = "Filtrar por fecha",
                Location = new Point(986, 12),
                AutoSize = true
            };

            dtDesde = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(1128, 10),
                Width = 120
            };

            dtHasta = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(1260, 10),
                Width = 120
            };

            toolbarPanel.Controls.AddRange(new Control[] {
                btnNuevaCompra, btnRegistrarProveedor, btnEvaluarProveedor,
                txtBuscar, cmbEstado, chkFecha, dtDesde, dtHasta
            });

            var contentCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(0)
            };
            contentCard.Paint += (s, e) => DrawCard(e.Graphics, contentCard.ClientRectangle);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false
            };

            lblRango = new Label
            {
                Text = "Mostrando 0 a 0 de 0 resultados",
                Dock = DockStyle.Bottom,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = ModernTheme.TextSecondary
            };

            var pagerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 12, 16, 12)
            };

            btnAnterior = ModernTheme.CreateGhostButton("Anterior");
            btnAnterior.Width = 120;
            btnAnterior.Location = new Point(8, 8);

            btnSiguiente = ModernTheme.CreateGhostButton("Siguiente");
            btnSiguiente.Width = 120;
            btnSiguiente.Location = new Point(140, 8);

            btnToggleDetalle = ModernTheme.CreateGhostButton("▶ Mostrar detalle");
            btnToggleDetalle.Width = 150;
            btnToggleDetalle.Location = new Point(272, 8);

            pagerPanel.Controls.AddRange(new Control[] { btnAnterior, btnSiguiente, btnToggleDetalle });

            pnlDetalle = BuildDetailPanel();

            contentCard.Controls.Add(grid);
            contentCard.Controls.Add(pnlDetalle);
            contentCard.Controls.Add(pagerPanel);
            contentCard.Controls.Add(lblRango);

            var rankingCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Margin = new Padding(0, 16, 0, 0)
            };
            rankingCard.Paint += (s, e) => DrawCard(e.Graphics, rankingCard.ClientRectangle);

            var rankingInner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 18, 24, 12)
            };

            var rankingHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            lblRankingTitulo = new Label
            {
                Text = "Ranking de desempeño de proveedores",
                Dock = DockStyle.Fill,
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            rankingHeader.Controls.Add(lblRankingTitulo);

            lblRankingResumen = new Label
            {
                Text = "Aún no hay evaluaciones registradas.",
                Dock = DockStyle.Bottom,
                Height = 28,
                ForeColor = ModernTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            gridProveedores = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false
            };

            rankingInner.Controls.Add(gridProveedores);
            rankingInner.Controls.Add(lblRankingResumen);
            rankingInner.Controls.Add(rankingHeader);

            rankingCard.Controls.Add(rankingInner);

            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260f));

            bodyLayout.Controls.Add(contentCard, 0, 0);
            bodyLayout.Controls.Add(rankingCard, 0, 1);

            container.Controls.Add(bodyLayout);
            container.Controls.Add(toolbarPanel);

            tabOrdenes.Controls.Add(container);
        }

        private void BuildTabProductos()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };

            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(0, 0, 0, 16)
            };

            txtBuscarProducto = new TextBox
            {
                PlaceholderText = "Buscar por código o nombre…",
                Width = 320,
                Location = new Point(0, 10)
            };

            cmbCategoriaProducto = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Location = new Point(332, 10)
            };

            chkSoloCritico = new CheckBox
            {
                Text = "Solo stock crítico (≤ 5)",
                Location = new Point(524, 12),
                AutoSize = true,
                ForeColor = ModernTheme.AccentWarning
            };

            btnNuevoProducto = ModernTheme.CreatePrimaryButton("＋ Nuevo producto");
            btnNuevoProducto.Location = new Point(720, 6);
            btnNuevoProducto.Width = 160;

            btnEditarProducto = ModernTheme.CreateSecondaryButton("Editar");
            btnEditarProducto.Location = new Point(892, 6);
            btnEditarProducto.Width = 100;

            btnEliminarProducto = ModernTheme.CreateGhostButton("Eliminar");
            btnEliminarProducto.Location = new Point(1004, 6);
            btnEliminarProducto.Width = 100;

            btnRefrescarProductos = ModernTheme.CreateGhostButton("Refrescar");
            btnRefrescarProductos.Location = new Point(1116, 6);
            btnRefrescarProductos.Width = 100;

            toolbarPanel.Controls.AddRange(new Control[] {
                txtBuscarProducto, cmbCategoriaProducto, chkSoloCritico,
                btnNuevoProducto, btnEditarProducto, btnEliminarProducto, btnRefrescarProductos
            });

            var contentCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(16)
            };
            contentCard.Paint += (s, e) => DrawCard(e.Graphics, contentCard.ClientRectangle);

            gridProductos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false
            };

            lblConteoProductos = new Label
            {
                Text = "Productos: 0",
                Dock = DockStyle.Bottom,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernTheme.TextSecondary,
                Padding = new Padding(0, 8, 0, 0)
            };

            contentCard.Controls.Add(gridProductos);
            contentCard.Controls.Add(lblConteoProductos);

            container.Controls.Add(contentCard);
            container.Controls.Add(toolbarPanel);

            tabProductos.Controls.Add(container);
        }

        private Panel BuildDetailPanel()
        {
            var detail = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                Padding = new Padding(24),
                BackColor = ModernTheme.Surface
            };
            detail.Paint += (s, e) => DrawCard(e.Graphics, detail.ClientRectangle);

            var lblHead = new Label
            {
                Text = "Detalle de la cotización",
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36
            };

            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true
            };
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            infoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            infoPanel.Controls.Add(CreateInfoLabel("N° Cotización"), 0, 0);
            lblOrd = CreateValueLabel();
            infoPanel.Controls.Add(lblOrd, 1, 0);

            infoPanel.Controls.Add(CreateInfoLabel("Proveedor"), 0, 1);
            lblProv = CreateValueLabel();
            infoPanel.Controls.Add(lblProv, 1, 1);

            infoPanel.Controls.Add(CreateInfoLabel("Fecha"), 0, 2);
            lblFecha = CreateValueLabel();
            infoPanel.Controls.Add(lblFecha, 1, 2);

            infoPanel.Controls.Add(CreateInfoLabel("Estado"), 0, 3);
            lblEstado = CreateValueLabel();
            infoPanel.Controls.Add(lblEstado, 1, 3);

            listProductos = new ListView
            {
                Dock = DockStyle.Top,
                Height = 220,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listProductos.Columns.Add("Producto", 160);
            listProductos.Columns.Add("Precio", 80);
            listProductos.Columns.Add("Cant.", 60);
            listProductos.Columns.Add("Subtotal", 80);

            var totalPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48
            };

            var lblTotalCaption = new Label
            {
                Text = "Total:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 80
            };

            lblTotal = new Label
            {
                Text = "$0.00",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = ModernTheme.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            totalPanel.Controls.Add(lblTotal);
            totalPanel.Controls.Add(lblTotalCaption);

            detail.Controls.Add(totalPanel);
            detail.Controls.Add(listProductos);
            detail.Controls.Add(infoPanel);
            detail.Controls.Add(lblHead);

            return detail;
        }

        private static Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(0, 6, 12, 6),
                AutoSize = true
            };
        }

        private static Label CreateValueLabel()
        {
            return new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Margin = new Padding(0, 6, 0, 6),
                AutoSize = true
            };
        }

        private static void DrawCard(Graphics g, Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 18;
            int diameter = radius * 2;

            if (bounds.Width < diameter || bounds.Height < diameter) return;

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