using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MyMarket_ERP
{
    //estado
    public partial class Central : Form
    {
        private readonly string _role;
        private bool _isLoggingOut = false;
        private int _currentChartPoints = 0;

        // Períodos disponibles para el combo
        private enum AnalyticsPeriod
        {
            Hoy,
            Ultimos7Dias,
            Ultimos30Dias,
            EsteAno
        }

        private const int ChartBlockWidth = 48;
        private const int ChartMinimumHeight = 300;

        public Central(string role)
        {
            _role = role ?? "admin";
            InitializeComponent();
            this.Tag = NavSection.Central;

            if (stockAlertsContainer != null)
                stockAlertsContainer.Resize += StockAlertsContainer_Resize;

            if (salesChart != null)
            {
                salesChart.MinimumSize = new Size(1, 1);
                if (salesChart.Height <= 0) salesChart.Height = 200;
            }

            Load += Central_Load;
            FormClosing += Central_FormClosing;

            Shown += (s, e) =>
{
    try
    {
        if (salesChart.Height <= 0) salesChart.Height = 200;

        // Inicializar combo en "Últimos 7 días" y cargar
        if (cbAnalyticsPeriod != null && cbAnalyticsPeriod.Items.Count > 0)
            cbAnalyticsPeriod.SelectedIndex = 0;

        // Cargar directamente el gráfico de Últimos 7 días
        LoadAnalytics(AnalyticsPeriod.Ultimos7Dias);

        LoadStockAlerts();
        PositionCloseButton();
    }
    catch { }
};


            Resize += (s, e) =>
            {
                if (salesChart != null && salesChart.Height <= 0)
                    salesChart.Height = 1;
                PositionCloseButton();
                UpdateChartViewport();
            };
        }

        private void Central_Load(object sender, EventArgs e)
        {
            SidebarInstaller.Install(
                this,
                _role,
                NavSection.Central,
                section => NavigationService.Open(section, this, _role)
            );

            lblRole.Text = $"Rol: {_role}";
            LoadKpis();
        }

        private void Central_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isLoggingOut) return;
            if (e.CloseReason != CloseReason.UserClosing) return;
        }

        private void PositionCloseButton()
        {
            if (headerPanel != null && btnCerrarSesion != null)
            {
                int rightMargin = 16;
                int topMargin = 8;
                btnCerrarSesion.Location = new Point(
                    headerPanel.Width - btnCerrarSesion.Width - rightMargin,
                    topMargin
                );
            }
        }

        private void UpdateChartViewport()
        {
            if (chartScrollContainer == null || salesChart == null)
                return;

            int visibleWidth = Math.Max(chartScrollContainer.ClientSize.Width, 1);
            int desiredWidth = _currentChartPoints > 0
                ? Math.Max(visibleWidth, _currentChartPoints * ChartBlockWidth)
                : visibleWidth;

            int desiredHeight = Math.Max(chartScrollContainer.ClientSize.Height, ChartMinimumHeight);

            salesChart.SuspendLayout();
            salesChart.Location = new Point(0, 0);
            salesChart.Size = new Size(desiredWidth, desiredHeight);
            salesChart.ResumeLayout();

            // Solo activar scroll si hace falta
            if (desiredWidth > visibleWidth)
                chartScrollContainer.AutoScrollMinSize = new Size(desiredWidth, desiredHeight);
            else
            {
                chartScrollContainer.AutoScrollMinSize = Size.Empty;
                chartScrollContainer.AutoScrollPosition = new Point(0, 0);
            }

            chartScrollContainer.VerticalScroll.Enabled = false;
        }

        private void SalesChart_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (chartScrollContainer == null) return;

            var scroll = chartScrollContainer.HorizontalScroll;
            if (!scroll.Visible && scroll.Maximum <= scroll.LargeChange) return;

            int newValue = scroll.Value - e.Delta;
            int maxValue = Math.Max(scroll.Minimum, scroll.Maximum - scroll.LargeChange + 1);
            if (newValue < scroll.Minimum) newValue = scroll.Minimum;
            else if (newValue > maxValue) newValue = maxValue;

            scroll.Value = newValue;
            chartScrollContainer.PerformLayout();
        }

        private void ChartScrollContainer_SizeChanged(object? sender, EventArgs e)
        {
            UpdateChartViewport();
        }

        private void StockAlertsContainer_Resize(object? sender, EventArgs e)
        {
            if (stockAlertsContainer == null) return;

            int availableWidth = Math.Max(320, stockAlertsContainer.ClientSize.Width - stockAlertsContainer.Padding.Horizontal);

            foreach (Control control in stockAlertsContainer.Controls)
            {
                control.Width = availableWidth;
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
        }

        private void LoadKpis()
        {
            try
            {
                using var cn = Database.OpenConnection();

                decimal totalToday = GetDecimal(cn, "SELECT ISNULL(SUM(Total),0) FROM dbo.Invoices WHERE CAST(IssuedAt AS DATE) = CAST(SYSDATETIME() AS DATE);");
                int invoicesToday = GetInt(cn, "SELECT COUNT(1) FROM dbo.Invoices WHERE CAST(IssuedAt AS DATE) = CAST(SYSDATETIME() AS DATE);");

                decimal last7Total = GetDecimal(cn, @"
                    SELECT ISNULL(SUM(Total),0)
                    FROM dbo.Invoices
                    WHERE IssuedAt >= DATEADD(DAY,-6, CAST(SYSDATETIME() AS DATE));");

                int last7Invoices = GetInt(cn, @"
                    SELECT COUNT(1)
                    FROM dbo.Invoices
                    WHERE IssuedAt >= DATEADD(DAY,-6, CAST(SYSDATETIME() AS DATE));");

                int lowStock = GetInt(cn, "SELECT COUNT(1) FROM dbo.Products WHERE IsActive = 1 AND Stock <= 5;");
                int totalProducts = GetInt(cn, "SELECT COUNT(1) FROM dbo.Products WHERE IsActive = 1;");
                int activeEmployees = GetInt(cn, "SELECT COUNT(1) FROM dbo.Employees WHERE Status = 'Activo';");
                int totalEmployees = GetInt(cn, "SELECT COUNT(1) FROM dbo.Employees;");

                cardVentas.Value = totalToday.ToString("C0");
                cardVentas.Meta = invoicesToday == 1
                    ? "1 venta registrada hoy"
                    : $"{invoicesToday} ventas registradas hoy";

                cardCompras.Value = last7Total.ToString("C0");
                cardCompras.Meta = last7Invoices == 1
                    ? "1 comprobante en la última semana"
                    : $"{last7Invoices} comprobantes en la última semana";

                cardStock.Value = lowStock.ToString();
                cardStock.Meta = totalProducts > 0
                    ? $"de {totalProducts} productos activos"
                    : "Sin productos activos";

                cardEmpleados.Value = activeEmployees.ToString();
                cardEmpleados.Meta = totalEmployees > 0
                    ? $"de {totalEmployees} colaboradores"
                    : "Sin registros de personal";
            }
            catch
            {
                cardVentas.Value = "-"; cardVentas.Meta = "Error al cargar";
                cardCompras.Value = "-"; cardCompras.Meta = "Error al cargar";
                cardStock.Value = "-"; cardStock.Meta = "";
                cardEmpleados.Value = "-"; cardEmpleados.Meta = "";
                lblAnalyticsSubtitle.Text = "Error al cargar métricas";
            }
        }

        private static decimal GetDecimal(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
        }

        private static int GetInt(SqlConnection cn, string sql)
        {
            using var cmd = new SqlCommand(sql, cn);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        // ===================== ANALYTICS =====================

        private void CbAnalyticsPeriod_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadAnalytics(GetSelectedPeriod());
        }

        private AnalyticsPeriod GetSelectedPeriod()
        {
            return cbAnalyticsPeriod?.SelectedIndex switch
            {
                0 => AnalyticsPeriod.Ultimos7Dias,
                1 => AnalyticsPeriod.Ultimos30Dias,
                2 => AnalyticsPeriod.EsteAno,
                _ => AnalyticsPeriod.Ultimos7Dias
            };
        }


        private void LoadAnalytics(AnalyticsPeriod period)
        {
            try
            {
                using var cn = Database.OpenConnection();

                salesChart.Series["Ingresos"].Points.Clear();
                salesChart.Series["Costos"].Points.Clear();

                var ca = salesChart.ChartAreas["Main"];
                // Mostrar cada categoría en X y sin grid vertical
                ca.AxisX.Interval = 1;
                ca.AxisX.IsMarginVisible = true;
                ca.AxisX.MajorGrid.Enabled = false;

                // ------- Período y dataset -------
                if (period == AnalyticsPeriod.Hoy)
                {
                    lblAnalyticsSubtitle.Text = "Hoy";

                    // Agregados del día
                    decimal ingresos = 0m;
                    using (var cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(Total),0)
                        FROM dbo.Invoices
                        WHERE CAST(IssuedAt AS DATE) = CAST(SYSDATETIME() AS DATE);", cn))
                    {
                        ingresos = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
                    }

                    decimal costos = 0m;
                    using (var cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(Subtotal),0)
                        FROM dbo.Invoices
                        WHERE CAST(IssuedAt AS DATE) = CAST(SYSDATETIME() AS DATE);", cn))
                    {
                        costos = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
                    }

                    // Misma presentación que el resto: barras (Ingresos) + línea (Costos)
                    salesChart.Series["Ingresos"].ChartType = SeriesChartType.Column;
                    salesChart.Series["Ingresos"]["PointWidth"] = "0.5";

                    salesChart.Series["Costos"].ChartType = SeriesChartType.Line;
                    salesChart.Series["Costos"].BorderWidth = 3;
                    salesChart.Series["Costos"].MarkerStyle = MarkerStyle.Circle;
                    salesChart.Series["Costos"].MarkerSize = 6;

                    string label = DateTime.Today.ToString("dd/MM");
                    salesChart.Series["Ingresos"].Points.AddXY(label, ingresos);
                    salesChart.Series["Costos"].Points.AddXY(label, costos);

                    _currentChartPoints = 1;
                }
                else if (period == AnalyticsPeriod.Ultimos7Dias || period == AnalyticsPeriod.Ultimos30Dias)
                {
                    int days = period == AnalyticsPeriod.Ultimos7Dias ? 7 : 30;
                    lblAnalyticsSubtitle.Text = period == AnalyticsPeriod.Ultimos7Dias ? "Últimos 7 días" : "Últimos 30 días";

                    var data = new Dictionary<DateTime, (decimal Ingresos, decimal Costos)>();
                    for (int i = days - 1; i >= 0; i--)
                    {
                        var d = DateTime.Today.AddDays(-i);
                        data[d] = (0m, 0m);
                    }

                    // Ingresos por día
                    using (var cmd = new SqlCommand(@"
                        SELECT CAST(IssuedAt AS DATE) AS Dia, SUM(Total) AS Total
                        FROM dbo.Invoices
                        WHERE IssuedAt >= DATEADD(DAY, @negDays, CAST(SYSDATETIME() AS DATE))
                        GROUP BY CAST(IssuedAt AS DATE);", cn))
                    {
                        cmd.Parameters.AddWithValue("@negDays", -(days - 1));
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            var d = rd.GetDateTime(0).Date;
                            var total = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1);
                            if (data.ContainsKey(d))
                            {
                                var cur = data[d];
                                data[d] = (total, cur.Costos);
                            }
                        }
                    }

                    // Costos por día
                    using (var cmd = new SqlCommand(@"
                        SELECT CAST(IssuedAt AS DATE) AS Dia, SUM(Subtotal) AS Subtotal
                        FROM dbo.Invoices
                        WHERE IssuedAt >= DATEADD(DAY, @negDays, CAST(SYSDATETIME() AS DATE))
                        GROUP BY CAST(IssuedAt AS DATE);", cn))
                    {
                        cmd.Parameters.AddWithValue("@negDays", -(days - 1));
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            var d = rd.GetDateTime(0).Date;
                            var subtotal = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1);
                            if (data.ContainsKey(d))
                            {
                                var cur = data[d];
                                data[d] = (cur.Ingresos, subtotal);
                            }
                        }
                    }

                    // Presentación
                    salesChart.Series["Ingresos"].ChartType = SeriesChartType.Column;
                    salesChart.Series["Ingresos"]["PointWidth"] = "0.5";
                    salesChart.Series["Costos"].ChartType = SeriesChartType.Line;
                    salesChart.Series["Costos"].BorderWidth = 3;
                    salesChart.Series["Costos"].MarkerStyle = MarkerStyle.Circle;
                    salesChart.Series["Costos"].MarkerSize = 6;

                    // Etiquetas
                    ca.AxisX.Interval = days <= 14 ? 1 : 0;
                    ca.AxisX.IsMarginVisible = true;

                    foreach (var kv in data.OrderBy(k => k.Key))
                    {
                        string label = kv.Key.ToString("dd/MM");
                        salesChart.Series["Ingresos"].Points.AddXY(label, kv.Value.Ingresos);
                        salesChart.Series["Costos"].Points.AddXY(label, kv.Value.Costos);
                    }

                    _currentChartPoints = data.Count;
                }
                else // EsteAno
                {
                    lblAnalyticsSubtitle.Text = "Este año";
                    int year = DateTime.Today.Year;

                    var data = Enumerable.Range(1, 12).ToDictionary(
                        m => m,
                        m => (Ingresos: 0m, Costos: 0m));

                    // Ingresos por mes
                    using (var cmd = new SqlCommand(@"
                        SELECT MONTH(IssuedAt) AS Mes, SUM(Total) AS Total
                        FROM dbo.Invoices
                        WHERE YEAR(IssuedAt) = @y
                        GROUP BY MONTH(IssuedAt);", cn))
                    {
                        cmd.Parameters.AddWithValue("@y", year);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            int m = rd.GetInt32(0);
                            decimal total = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1);
                            var cur = data[m];
                            data[m] = (total, cur.Costos);
                        }
                    }

                    // Costos por mes
                    using (var cmd = new SqlCommand(@"
                        SELECT MONTH(IssuedAt) AS Mes, SUM(Subtotal) AS Subtotal
                        FROM dbo.Invoices
                        WHERE YEAR(IssuedAt) = @y
                        GROUP BY MONTH(IssuedAt);", cn))
                    {
                        cmd.Parameters.AddWithValue("@y", year);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            int m = rd.GetInt32(0);
                            decimal subtotal = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1);
                            var cur = data[m];
                            data[m] = (cur.Ingresos, subtotal);
                        }
                    }

                    var monthNames = System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedMonthNames;

                    // Presentación
                    salesChart.Series["Ingresos"].ChartType = SeriesChartType.Column;
                    salesChart.Series["Ingresos"]["PointWidth"] = "0.5";
                    salesChart.Series["Costos"].ChartType = SeriesChartType.Line;
                    salesChart.Series["Costos"].BorderWidth = 3;
                    salesChart.Series["Costos"].MarkerStyle = MarkerStyle.Circle;
                    salesChart.Series["Costos"].MarkerSize = 6;

                    ca.AxisX.Interval = 1;
                    ca.AxisX.IsMarginVisible = true;

                    for (int m = 1; m <= 12; m++)
                    {
                        string label = monthNames[m - 1];
                        salesChart.Series["Ingresos"].Points.AddXY(label, data[m].Ingresos);
                        salesChart.Series["Costos"].Points.AddXY(label, data[m].Costos);
                    }

                    _currentChartPoints = 12;
                }

                UpdateChartViewport();
            }
            catch
            {
                salesChart.Series["Ingresos"].Points.Clear();
                salesChart.Series["Costos"].Points.Clear();
                lblAnalyticsSubtitle.Text = "Error al cargar datos";
                _currentChartPoints = 0;
                UpdateChartViewport();
            }
        }

        // ====== NUEVA FUNCIÓN: Alertas de Stock Crítico ======
        private void LoadStockAlerts()
        {
            try
            {
                if (stockAlertsContainer == null || stockAlertsContainer.IsDisposed)
                    return;

                stockAlertsContainer.SuspendLayout();
                stockAlertsContainer.Controls.Clear();

                using var cn = Database.OpenConnection();

                var stockCritico = new List<(int ProductId, string Code, string Name, int Stock)>();
                using (var cmd = new SqlCommand(@"
                    SELECT Id, Code, Name, Stock
                    FROM dbo.Products
                    WHERE IsActive = 1 AND Stock <= 5
                    ORDER BY Stock ASC, Name ASC;", cn))
                {
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        stockCritico.Add((
                            rd.GetInt32(0),
                            rd.GetString(1),
                            rd.GetString(2),
                            rd.GetInt32(3)
                        ));
                    }
                }

                if (stockCritico.Count > 0)
                {
                    PurchaseOrderRepository.EnsureAutoDraftsForCriticalStock(stockCritico);
                }

                Control card = stockCritico.Count > 0
                    ? CreateStockAlertCard(stockCritico)
                    : CreateStockMessageCard(
                        "Stock al día",
                        "Todos los productos activos se encuentran por encima del umbral crítico.",
                        ModernTheme.AccentSuccess,
                        IconGlyphs.Checkmark
                    );

                stockAlertsContainer.Controls.Add(card);
                StockAlertsContainer_Resize(stockAlertsContainer, EventArgs.Empty);
                stockAlertsContainer.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                try
                {
                    if (stockAlertsContainer != null && !stockAlertsContainer.IsDisposed)
                    {
                        stockAlertsContainer.Controls.Clear();
                        var alertError = CreateStockMessageCard(
                            "Error al cargar stock",
                            $"No se pudieron cargar las alertas: {ex.Message}",
                            ModernTheme.AccentDanger,
                            IconGlyphs.Warning
                        );
                        stockAlertsContainer.Controls.Add(alertError);
                        StockAlertsContainer_Resize(stockAlertsContainer, EventArgs.Empty);
                        stockAlertsContainer.ResumeLayout(true);
                    }
                }
                catch { }
            }
        }

        private Panel CreateStockAlertCard(List<(int ProductId, string Code, string Name, int Stock)> productos)
        {
            var container = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 0, 0),
                BackColor = ModernTheme.Surface,
                MinimumSize = new Size(0, 260)
            };
            container.Paint += (s, e) => DrawAlertCard(e.Graphics, container.ClientRectangle);

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // header
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // lista
            contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // footer

            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 64,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            var iconLabel = new Label
            {
                Text = IconGlyphs.Warning,
                Font = new Font("Segoe MDL2 Assets", 28f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = ModernTheme.AccentWarning,
                Width = 56,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 12, 0)
            };

            var titlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var lblTitle = new Label
            {
                Text = "Stock crítico",
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblCount = new Label
            {
                Text = productos.Count == 1
                    ? "1 producto requiere reposición inmediata"
                    : $"{productos.Count} productos requieren reposición inmediata",
                Font = ModernTheme.Caption,
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24
            };

            titlePanel.Controls.Add(lblCount);
            titlePanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(titlePanel);
            headerPanel.Controls.Add(iconLabel);

            var listPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };

            var headerRow = new TableLayoutPanel
            {
                ColumnCount = 3,
                Height = 28,
                Margin = new Padding(0),
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.Transparent
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            headerRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            headerRow.Controls.Add(new Label
            {
                Text = "Código",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary
            }, 0, 0);

            headerRow.Controls.Add(new Label
            {
                Text = "Producto",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary
            }, 1, 0);

            headerRow.Controls.Add(new Label
            {
                Text = "Stock",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary
            }, 2, 0);

            listPanel.Controls.Add(headerRow);

            foreach (var prod in productos.Take(12))
            {
                var row = new TableLayoutPanel
                {
                    ColumnCount = 3,
                    Height = 36,
                    Margin = new Padding(0, 4, 0, 0),
                    Padding = new Padding(8, 4, 8, 4),
                    BackColor = prod.Stock == 0
                        ? Color.FromArgb(254, 226, 226)
                        : Color.FromArgb(254, 243, 199)
                };
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                row.Controls.Add(new Label
                {
                    Text = prod.Code,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = ModernTheme.TextPrimary
                }, 0, 0);

                row.Controls.Add(new Label
                {
                    Text = prod.Name,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = ModernTheme.TextSecondary,
                    AutoEllipsis = true
                }, 1, 0);

                row.Controls.Add(new Label
                {
                    Text = prod.Stock == 0 ? "Agotado" : $"{prod.Stock} unidades",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = prod.Stock == 0
                        ? Color.FromArgb(185, 28, 28)
                        : Color.FromArgb(180, 83, 9)
                }, 2, 0);

                listPanel.Controls.Add(row);
            }

            if (productos.Count > 12)
            {
                var lblMore = new Label
                {
                    Text = $"+ {productos.Count - 12} productos adicionales en stock crítico",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = ModernTheme.TextSecondary,
                    Margin = new Padding(8, 8, 0, 0),
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                listPanel.Controls.Add(lblMore);
            }

            var footerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0, 12, 0, 0),
                BackColor = Color.Transparent
            };

            var linkInventario = new LinkLabel
            {
                Text = "Abrir inventario",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                LinkColor = ModernTheme.AccentSecondary,
                ActiveLinkColor = ModernTheme.Accent,
                AutoSize = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Margin = new Padding(0)
            };
            linkInventario.LinkClicked += (s, e) => btnAbrirClientes_Click(s, e);
            footerPanel.Controls.Add(linkInventario);

            contentLayout.Controls.Add(headerPanel, 0, 0);
            contentLayout.Controls.Add(listPanel, 0, 1);
            contentLayout.Controls.Add(footerPanel, 0, 2);

            container.Controls.Add(contentLayout);

            void AdjustWidths()
            {
                int width = Math.Max(320, container.ClientSize.Width - container.Padding.Horizontal);
                foreach (Control control in listPanel.Controls)
                    control.Width = width;
            }

            container.Resize += (s, e) => AdjustWidths();
            listPanel.ControlAdded += (s, e) => AdjustWidths();
            AdjustWidths();

            return container;
        }

        private Panel CreateStockMessageCard(string title, string description, Color accentColor, string glyph)
        {
            var container = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 0, 0),
                BackColor = ModernTheme.Surface,
                MinimumSize = new Size(0, 180)
            };
            container.Paint += (s, e) => DrawAlertCard(e.Graphics, container.ClientRectangle);

            var icon = new Label
            {
                Text = glyph,
                Font = new Font("Segoe MDL2 Assets", 36f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = accentColor,
                Width = 72,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 16, 0)
            };

            var textPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 32
            };

            var lblDescription = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Fill
            };

            textPanel.Controls.Add(lblDescription);
            textPanel.Controls.Add(lblTitle);

            container.Controls.Add(textPanel);
            container.Controls.Add(icon);

            return container;
        }

        private static void DrawAlertCard(Graphics g, Rectangle bounds)
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

        private void btnAbrirClientes_Click(object sender, EventArgs e)
        {
            using var cli = new Inventario();
            cli.StartPosition = FormStartPosition.CenterParent;
            cli.ShowDialog(this);
        }

        private async void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            _isLoggingOut = true;
            btnCerrarSesion.Enabled = false;
            try
            {
                AppSession.Clear();    // limpia sesión/estado
            }
            catch
            {
           
            }
            Close();        
            this.Close();
        }
    }
}