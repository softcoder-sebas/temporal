using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class Contabilidad : Form
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblTitulo;
        private Button btnNuevoAsiento;
        private Button btnRefrescar;
        private Button btnExportar;
        private CheckBox chkRango;
        private DateTimePicker dtDesde;
        private DateTimePicker dtHasta;

        private TabControl tab;
        private TabPage tabLibro;
        private TabPage tabBalance;
        private TabPage tabResultados;
        private DataGridView gridLibro;
        private DataGridView gridBalance;
        private DataGridView gridResultados;
        private Label lblLibroTotales;
        private Label lblBalanceTotales;
        private Label lblResultados;

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
            Text = "Módulo Contable";

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
            layoutRoot.Controls.Add(tab, 0, 1);

            Controls.Add(layoutRoot);
        }

        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 16)
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Título
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Botón Nuevo asiento
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Controles secundarios

            // === FILA 1: Título ===
            lblTitulo = new Label
            {
                Text = "Contabilidad",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            headerLayout.Controls.Add(lblTitulo, 0, 0);

            // === FILA 2: Botón Nuevo asiento ===
            var nuevoAsientoPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(0)
            };

            btnNuevoAsiento = ModernTheme.CreatePrimaryButton("＋ Nuevo asiento");
            btnNuevoAsiento.Width = 160;
            btnNuevoAsiento.Margin = new Padding(0, 0, 0, 0);

            nuevoAsientoPanel.Controls.Add(btnNuevoAsiento);
            headerLayout.Controls.Add(nuevoAsientoPanel, 0, 1);

            // === FILA 3: Controles secundarios ===
            var controlBar = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            btnRefrescar = ModernTheme.CreateGhostButton("Refrescar");
            btnRefrescar.Width = 110;
            btnRefrescar.Margin = new Padding(0, 0, 12, 12);

            btnExportar = ModernTheme.CreateGhostButton("Exportar Excel");
            btnExportar.Width = 130;
            btnExportar.Margin = new Padding(0, 0, 24, 12);

            chkRango = new CheckBox
            {
                Text = "Rango de fechas",
                AutoSize = true,
                Margin = new Padding(0, 8, 12, 12)
            };

            dtDesde = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Margin = new Padding(0, 4, 12, 12)
            };

            dtHasta = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Margin = new Padding(0, 4, 12, 12)
            };

            controlBar.Controls.Add(btnRefrescar);
            controlBar.Controls.Add(btnExportar);
            controlBar.Controls.Add(chkRango);
            controlBar.Controls.Add(dtDesde);
            controlBar.Controls.Add(dtHasta);

            headerLayout.Controls.Add(controlBar, 0, 2);

            topBar.Controls.Add(headerLayout);
        }

        private void BuildTabs()
        {
            tab = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                Font = ModernTheme.Body
            };

            tabLibro = new TabPage("Libro Diario");
            tabBalance = new TabPage("Balance General");
            tabResultados = new TabPage("Estado de Resultados");

            gridLibro = CreateGrid();
            gridBalance = CreateGrid();
            gridResultados = CreateGrid();

            lblLibroTotales = CreateTotalsLabel();
            lblBalanceTotales = CreateTotalsLabel();
            lblResultados = CreateTotalsLabel();

            tabLibro.Controls.Add(BuildTabCard(gridLibro, lblLibroTotales));
            tabBalance.Controls.Add(BuildTabCard(gridBalance, lblBalanceTotales));
            tabResultados.Controls.Add(BuildTabCard(gridResultados, lblResultados));

            tab.TabPages.Add(tabLibro);
            tab.TabPages.Add(tabBalance);
            tab.TabPages.Add(tabResultados);
        }

        private static Panel BuildTabCard(Control mainGrid, Label footer)
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(0)
            };
            card.Paint += (s, e) => DrawCard(e.Graphics, card.ClientRectangle);

            mainGrid.Dock = DockStyle.Fill;
            footer.Dock = DockStyle.Bottom;
            footer.Height = 32;
            footer.TextAlign = ContentAlignment.MiddleRight;
            footer.Padding = new Padding(0, 0, 24, 0);

            card.Controls.Add(mainGrid);
            card.Controls.Add(footer);
            container.Controls.Add(card);
            return container;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None
            };
            ModernTheme.StyleDataGrid(grid);
            return grid;
        }

        private static Label CreateTotalsLabel()
        {
            return new Label
            {
                Text = "Total: 0",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary
            };
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