using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Historial_facturacion
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel headerPanel;
        private Label lblTitulo;
        private Button btnRefrescar;
        private Label lblClienteSelector;
        private ComboBox cboCliente;
        private Label lblClienteActual;

        private TableLayoutPanel filtersPanel;
        private TextBox txtBuscar;
        private ComboBox cboEstado;
        private ComboBox cboMetodoPago;
        private DateTimePicker dtDesde;
        private DateTimePicker dtHasta;
        private Label lblBuscar;
        private Label lblEstado;
        private Label lblMetodo;
        private Label lblDesde;
        private Label lblHasta;

        private Panel summaryPanel;
        private Label lblTotalTitulo;
        private Label lblTotalGastado;
        private Label lblClienteResumenTitulo;
        private Label lblClienteResumen;

        private Label lblMensaje;
        private Panel invoiceInfoPanel;  // NUEVO: Panel para info de factura seleccionada
        private Label lblDetalleTitulo;
        private Label lblDetalleInfo;

        private SplitContainer splitMain;
        private DataGridView gridFacturas;
        private Panel detailPanel;
        private DataGridView gridDetalle;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _searchTimer?.Dispose();
            }
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
            Text = "Historial de facturación";

            layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,  // CAMBIO: Ahora son 6 filas
                Padding = new Padding(24),
                BackColor = Color.Transparent
            };
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Header
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Filters
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Summary
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Message
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // NUEVO: Invoice Info
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Content

            BuildHeader();
            BuildFilters();
            BuildSummary();
            BuildInvoiceInfo();  // NUEVO
            BuildContent();

            layoutRoot.Controls.Add(headerPanel, 0, 0);
            layoutRoot.Controls.Add(filtersPanel, 0, 1);
            layoutRoot.Controls.Add(summaryPanel, 0, 2);
            layoutRoot.Controls.Add(lblMensaje, 0, 3);
            layoutRoot.Controls.Add(invoiceInfoPanel, 0, 4);  // NUEVO
            layoutRoot.Controls.Add(splitMain, 0, 5);  // CAMBIO: ahora es fila 5

            Controls.Add(layoutRoot);
        }

        private void BuildHeader()
        {
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            lblTitulo = new Label
            {
                Text = "Historial de facturación y compras",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            btnRefrescar = ModernTheme.CreateGhostButton("Refrescar");
            btnRefrescar.Location = new Point(0, 40);

            lblClienteSelector = new Label
            {
                Text = "Cliente",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(180, 48)
            };

            cboCliente = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 260,
                Location = new Point(240, 44)
            };

            lblClienteActual = new Label
            {
                Text = "",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(180, 48),
                Visible = false
            };

            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(btnRefrescar);
            headerPanel.Controls.Add(lblClienteSelector);
            headerPanel.Controls.Add(cboCliente);
            headerPanel.Controls.Add(lblClienteActual);
        }

        private void BuildFilters()
        {
            filtersPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };
            filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));
            filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));
            filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));
            filtersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.5f));

            lblBuscar = new Label
            {
                Text = "Buscar",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true
            };
            txtBuscar = new TextBox
            {
                PlaceholderText = "Factura, producto o código…",
                Width = 360,
                Dock = DockStyle.Fill
            };

            lblEstado = new Label
            {
                Text = "Estado",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true
            };
            cboEstado = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Dock = DockStyle.Fill
            };

            lblMetodo = new Label
            {
                Text = "Método",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true
            };
            cboMetodoPago = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Dock = DockStyle.Fill
            };

            lblDesde = new Label
            {
                Text = "Desde",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true
            };
            dtDesde = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 160,
                Dock = DockStyle.Fill
            };

            lblHasta = new Label
            {
                Text = "Hasta",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true
            };
            dtHasta = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 160,
                Dock = DockStyle.Fill
            };

            filtersPanel.Controls.Add(lblBuscar, 0, 0);
            filtersPanel.Controls.Add(lblEstado, 1, 0);
            filtersPanel.Controls.Add(lblMetodo, 2, 0);
            filtersPanel.Controls.Add(lblDesde, 3, 0);
            filtersPanel.Controls.Add(lblHasta, 4, 0);

            filtersPanel.Controls.Add(txtBuscar, 0, 1);
            filtersPanel.Controls.Add(cboEstado, 1, 1);
            filtersPanel.Controls.Add(cboMetodoPago, 2, 1);
            filtersPanel.Controls.Add(dtDesde, 3, 1);
            filtersPanel.Controls.Add(dtHasta, 4, 1);
        }

        private void BuildSummary()
        {
            summaryPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(0, 12, 0, 12),
                BackColor = Color.Transparent
            };

            lblTotalTitulo = new Label
            {
                Text = "Total gastado",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            lblTotalGastado = new Label
            {
                Text = "$0",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.Accent,
                AutoSize = true,
                Location = new Point(0, 20)
            };

            lblClienteResumenTitulo = new Label
            {
                Text = "Cliente",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(260, 0)
            };

            lblClienteResumen = new Label
            {
                Text = "",
                Font = ModernTheme.Body,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(260, 20)
            };

            lblMensaje = new Label
            {
                Text = "",
                Font = ModernTheme.Body,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Visible = false,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 8, 0, 8),
                Margin = new Padding(0, 0, 0, 12)
            };

            summaryPanel.Controls.Add(lblTotalTitulo);
            summaryPanel.Controls.Add(lblTotalGastado);
            summaryPanel.Controls.Add(lblClienteResumenTitulo);
            summaryPanel.Controls.Add(lblClienteResumen);
        }

        // NUEVO MÉTODO
        private void BuildInvoiceInfo()
        {
            invoiceInfoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(0, 12, 0, 12),
                BackColor = Color.Transparent,
                Visible = false  // Oculto por defecto
            };

            lblDetalleTitulo = new Label
            {
                Text = "Factura seleccionada",
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            lblDetalleInfo = new Label
            {
                Text = "Selecciona una factura para ver el detalle.",
                Font = ModernTheme.Body,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 28)
            };

            invoiceInfoPanel.Controls.Add(lblDetalleTitulo);
            invoiceInfoPanel.Controls.Add(lblDetalleInfo);
        }

        private void BuildContent()
        {
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Color.Transparent,
                SplitterWidth = 6
            };

            gridFacturas = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.Surface,
                BorderStyle = BorderStyle.None
            };

            splitMain.Panel1.Controls.Add(gridFacturas);

            detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(16)
            };

            gridDetalle = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.Surface,
                BorderStyle = BorderStyle.None
            };

            detailPanel.Controls.Add(gridDetalle);
            splitMain.Panel2.Controls.Add(detailPanel);
        }
    }
}