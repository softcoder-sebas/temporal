using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MyMarket_ERP
{
    partial class Central
    {
        private System.ComponentModel.IContainer components = null;

        private Panel contentHost;
        private TableLayoutPanel layoutRoot;
        private Panel headerPanel;
        private Label lblTitulo;
        private Label lblRole;
        private Button btnCerrarSesion;

        private TableLayoutPanel metricsLayout;
        private ModernCard cardVentas;
        private ModernCard cardCompras;
        private ModernCard cardStock;
        private ModernCard cardEmpleados;

        private Panel analyticsPanel;
        private Panel analyticsCard;
        private Panel analyticsHeaderRow;
        private Label lblAnalyticsTitle;
        private ComboBox cbAnalyticsPeriod;
        private Label lblAnalyticsSubtitle;
        private Panel chartScrollContainer;
        private Chart salesChart;

        private Panel stockAlertsContainer;

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
            ClientSize = new Size(1280, 800);
            Font = ModernTheme.Body;
            ForeColor = ModernTheme.TextPrimary;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MyMarket ERP - Central";

            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoScrollMargin = new Size(0, 24),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(24, 24, 24, 16),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layoutRoot.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutRoot.ColumnStyles.Clear();
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutRoot.RowStyles.Clear();
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPIs
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Chart
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Alerts

            // Header del formulario
            headerPanel = new Panel
            {
                Name = "FormHeaderPanel",
                Dock = DockStyle.Fill,
                Height = 80,
                Padding = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            lblTitulo = new Label
            {
                Name = "FormHeaderTitle",
                Text = "Panel de control",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            lblRole = new Label
            {
                Name = "FormHeaderRole",
                Text = "Admin",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(2, 36)
            };

            btnCerrarSesion = ModernTheme.CreateGhostButton("Cerrar sesión");
            btnCerrarSesion.BackColor = Color.FromArgb(239, 68, 68);
            btnCerrarSesion.ForeColor = Color.White;
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            btnCerrarSesion.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
            btnCerrarSesion.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 28, 28);
            btnCerrarSesion.FlatAppearance.BorderColor = Color.FromArgb(185, 28, 28);
            btnCerrarSesion.Height = 36;
            btnCerrarSesion.Width = 150;
            btnCerrarSesion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCerrarSesion.Click += btnCerrarSesion_Click;

            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(lblRole);
            headerPanel.Controls.Add(btnCerrarSesion);

            // KPIs
            metricsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 16),
                AutoSize = true,
                Height = 140
            };
            metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            cardVentas = new ModernCard
            {
                Title = "Ventas del día",
                Value = "$0",
                Meta = "Cargando...",
                Dock = DockStyle.Fill,
                IconGlyph = IconGlyphs.Shop,
                IconToolTip = "Ventas del día",
                MinimumSize = new Size(0, 130)
            };
            cardVentas.AccentColor = ModernTheme.Accent;

            cardCompras = new ModernCard
            {
                Title = "Compras recientes",
                Value = "$0",
                Meta = "Cargando...",
                Dock = DockStyle.Fill,
                IconGlyph = IconGlyphs.Cart,
                IconToolTip = "Compras recientes",
                MinimumSize = new Size(0, 130)
            };
            cardCompras.AccentColor = ModernTheme.AccentSecondary;

            cardStock = new ModernCard
            {
                Title = "Stock crítico",
                Value = "0",
                Meta = "productos",
                Dock = DockStyle.Fill,
                IconGlyph = IconGlyphs.Boxes,
                IconToolTip = "Stock crítico",
                MinimumSize = new Size(0, 130)
            };
            cardStock.AccentColor = ModernTheme.AccentWarning;

            cardEmpleados = new ModernCard
            {
                Title = "Empleados activos",
                Value = "0",
                Meta = "colaboradores",
                Dock = DockStyle.Fill,
                IconGlyph = IconGlyphs.PeopleTeam,
                IconToolTip = "Empleados activos",
                MinimumSize = new Size(0, 130)
            };
            cardEmpleados.AccentColor = ModernTheme.AccentSecondary;

            metricsLayout.Controls.Add(cardVentas, 0, 0);
            metricsLayout.Controls.Add(cardCompras, 1, 0);
            metricsLayout.Controls.Add(cardStock, 2, 0);
            metricsLayout.Controls.Add(cardEmpleados, 3, 0);

            // Analytics Panel
            analyticsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 16),
                Height = 420
            };

            analyticsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(24),
                Margin = new Padding(0),
                BorderStyle = BorderStyle.None
            };
            analyticsCard.Paint += (s, e) => DrawCard(e.Graphics, analyticsCard.ClientRectangle);

            // Header row (título + combo)
            analyticsHeaderRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            lblAnalyticsTitle = new Label
            {
                Text = "Ingresos vs Costos",
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cbAnalyticsPeriod = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Width = 160,
                Dock = DockStyle.Right,
                Margin = new Padding(0),
            };
            cbAnalyticsPeriod.Items.AddRange(new object[]
            {
                "Últimos 7 días",
                "Últimos 30 días",
                "Este año",
            });
            cbAnalyticsPeriod.SelectedIndexChanged += CbAnalyticsPeriod_SelectedIndexChanged;

            analyticsHeaderRow.Controls.Add(cbAnalyticsPeriod);
            analyticsHeaderRow.Controls.Add(lblAnalyticsTitle);

            lblAnalyticsSubtitle = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Top,
                AutoSize = true,                 // evita recorte de “Hoy”
                Padding = new Padding(0, 0, 0, 8)
            };

            chartScrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoScrollMargin = new Size(24, 0),
                AutoScrollMinSize = new Size(0, 320),
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TabStop = false
            };
            chartScrollContainer.SizeChanged += ChartScrollContainer_SizeChanged;
            chartScrollContainer.HorizontalScroll.SmallChange = 32;
            chartScrollContainer.HorizontalScroll.LargeChange = 160;

            salesChart = new Chart
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = Color.Transparent,
                Palette = ChartColorPalette.None,
                PaletteCustomColors = new[] { ModernTheme.Accent, Color.FromArgb(239, 68, 68) },
                MinimumSize = new Size(300, 320),
                Size = new Size(600, 320),
                Location = new Point(0, 0),
                Margin = new Padding(0)
            };

            var chartArea = new ChartArea("Main")
            {
                BackColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            chartArea.AxisX.MajorGrid.Enabled = false;
            chartArea.AxisX.LabelStyle.Font = new Font("Segoe UI", 9f);
            chartArea.AxisX.LabelStyle.Angle = 0; // legible
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chartArea.AxisX.LineColor = ModernTheme.Border;
            chartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chartArea.AxisY.MajorGrid.LineColor = ModernTheme.Border;
            chartArea.AxisY.LabelStyle.Font = new Font("Segoe UI", 9f);
            chartArea.AxisY.LabelStyle.Format = "C0";
            chartArea.AxisY.LineColor = ModernTheme.Border;
            salesChart.ChartAreas.Add(chartArea);
            salesChart.MouseWheel += SalesChart_MouseWheel;

            var legend = new Legend("Legend")
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent
            };
            salesChart.Legends.Add(legend);

            var seriesIngresos = new Series("Ingresos")
            {
                ChartType = SeriesChartType.Column,
                ChartArea = "Main",
                BorderWidth = 0,
                Color = ModernTheme.Accent,
                XValueType = ChartValueType.String,   // <--- importante
                IsXValueIndexed = true                // <--- importante
            };

            var seriesCostos = new Series("Costos")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "Main",
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6,
                Color = Color.FromArgb(239, 68, 68),
                XValueType = ChartValueType.String,   // <--- importante
                IsXValueIndexed = true                // <--- importante
            };


            salesChart.Series.Add(seriesIngresos);
            salesChart.Series.Add(seriesCostos);

            chartScrollContainer.Controls.Add(salesChart);

            // Orden dentro de la tarjeta
            analyticsCard.Controls.Add(chartScrollContainer);
            analyticsCard.Controls.Add(lblAnalyticsSubtitle);
            analyticsCard.Controls.Add(analyticsHeaderRow);

            analyticsPanel.Controls.Add(analyticsCard);

            // Stock alerts container
            stockAlertsContainer = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            layoutRoot.Controls.Add(headerPanel, 0, 0);
            layoutRoot.Controls.Add(metricsLayout, 0, 1);
            layoutRoot.Controls.Add(analyticsPanel, 0, 2);
            layoutRoot.Controls.Add(stockAlertsContainer, 0, 3);

            contentHost.Controls.Add(layoutRoot);

            Controls.Add(contentHost);
        }

        private static void DrawCard(Graphics g, Rectangle bounds)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 16;
            int d = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            using var brush = new SolidBrush(ModernTheme.Surface);
            using var border = new Pen(ModernTheme.Border);
            g.FillPath(brush, path);
            g.DrawPath(border, path);
        }

    }
}
