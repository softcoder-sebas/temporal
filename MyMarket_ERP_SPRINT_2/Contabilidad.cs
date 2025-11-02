using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace MyMarket_ERP
{
    public partial class Contabilidad : Form
    {
        // Libro diario en memoria
        private readonly DataTable _libro = new();

        // Mapas simples de cuentas (Tipo define signo contable)
        // Activo / Gasto: saldo = Debe - Haber
        // Pasivo / Patrimonio / Ingreso: saldo = Haber - Debe
        private readonly (string Codigo, string Nombre, string Tipo)[] _plan = new[]
        {
            ("1-01","Caja","Activo"),
            ("1-02","Bancos","Activo"),
            ("1-03","Inventarios","Activo"),
            ("2-01","Proveedores","Pasivo"),
            ("3-01","Capital","Patrimonio"),
            ("4-01","Ventas","Ingreso"),
            ("5-01","Costo de Ventas","Gasto"),
            ("5-02","Gastos Operativos","Gasto"),
        };

        public Contabilidad()
        {
            InitializeComponent();
            var role = AppSession.Role;
            this.Tag = NavSection.Contabilidad;

            SidebarInstaller.Install(
                this,
                role,
                NavSection.Contabilidad,
                section => NavigationService.Open(section, this, role)
            );
            BuildLibroSchema();
            SeedDemo();
            WireEvents();
            ApplyFiltersAndRefreshAll();
        }

        private void WireEvents()
        {
            btnRefrescar.Click += (_, __) => ApplyFiltersAndRefreshAll();
            btnExportar.Click += async (_, __) => await ExportActiveTabToExcelAsync();
            btnNuevoAsiento.Click += (_, __) => NuevoAsiento();

            chkRango.CheckedChanged += (_, __) => ApplyFiltersAndRefreshAll();
            dtDesde.ValueChanged += (_, __) => { if (chkRango.Checked) ApplyFiltersAndRefreshAll(); };
            dtHasta.ValueChanged += (_, __) => { if (chkRango.Checked) ApplyFiltersAndRefreshAll(); };
        }

        private void BuildLibroSchema()
        {
            _libro.Columns.Add("Fecha", typeof(DateTime));
            _libro.Columns.Add("CodCuenta", typeof(string));
            _libro.Columns.Add("Cuenta", typeof(string));
            _libro.Columns.Add("TipoCuenta", typeof(string)); // Activo, Pasivo, ...
            _libro.Columns.Add("Descripción", typeof(string));
            _libro.Columns.Add("Debe", typeof(decimal));
            _libro.Columns.Add("Haber", typeof(decimal));
        }

        private void SeedDemo()
        {
            // Asiento de venta: Cliente paga en efectivo, reconoce ingreso y costo
            AddRow(DateTime.Today.AddDays(-3), "1-01", "Caja", "Activo", "Venta mostrador FAC-001", 300_000m, 0m);
            AddRow(DateTime.Today.AddDays(-3), "4-01", "Ventas", "Ingreso", "Venta mostrador FAC-001", 0m, 300_000m);

            AddRow(DateTime.Today.AddDays(-3), "5-01", "Costo de Ventas", "Gasto", "Salida de inventario", 180_000m, 0m);
            AddRow(DateTime.Today.AddDays(-3), "1-03", "Inventarios", "Activo", "Salida de inventario", 0m, 180_000m);

            // Compra a proveedor a crédito
            AddRow(DateTime.Today.AddDays(-2), "1-03", "Inventarios", "Activo", "Compra OC-100", 250_000m, 0m);
            AddRow(DateTime.Today.AddDays(-2), "2-01", "Proveedores", "Pasivo", "Compra OC-100", 0m, 250_000m);

            // Aporte de capital en banco
            AddRow(DateTime.Today.AddDays(-1), "1-02", "Bancos", "Activo", "Aporte de socios", 800_000m, 0m);
            AddRow(DateTime.Today.AddDays(-1), "3-01", "Capital", "Patrimonio", "Aporte de socios", 0m, 800_000m);

            // Gasto operativo
            AddRow(DateTime.Today, "5-02", "Gastos Operativos", "Gasto", "Servicios públicos", 90_000m, 0m);
            AddRow(DateTime.Today, "1-02", "Bancos", "Activo", "Servicios públicos", 0m, 90_000m);
        }

        private void AddRow(DateTime fecha, string cod, string nombre, string tipo, string desc, decimal debe, decimal haber)
        {
            var r = _libro.NewRow();
            r["Fecha"] = fecha;
            r["CodCuenta"] = cod;
            r["Cuenta"] = nombre;
            r["TipoCuenta"] = tipo;
            r["Descripción"] = desc;
            r["Debe"] = debe;
            r["Haber"] = haber;
            _libro.Rows.Add(r);
        }

        // ====== Refrescos ======

        private void ApplyFiltersAndRefreshAll()
        {
            // 1) Libro (con filtro de fechas)
            var libroFiltrado = _libro.AsEnumerable().AsQueryable();

            if (chkRango.Checked)
            {
                var d1 = dtDesde.Value.Date;
                var d2 = dtHasta.Value.Date;
                if (d2 < d1) d2 = d1;
                libroFiltrado = libroFiltrado.Where(r => r.Field<DateTime>("Fecha").Date >= d1 &&
                                                         r.Field<DateTime>("Fecha").Date <= d2);
            }

            // libroFiltrado es un DataTable (o DataView). Conviértelo a IEnumerable<DataRow>.
            var tLibro = libroFiltrado
                .AsEnumerable()                               // <- importante
                .OrderBy(r => r.Field<DateTime>("Fecha"))
                .CopyToDataTableOrEmpty();                    // helper robusto


            gridLibro.DataSource = tLibro;
            FormatLibroGrid();
            PutLibroTotals(tLibro);

            // 2) Balance (saldos por cuenta y totales por tipo)
            var tBalance = BuildBalanceTable(tLibro);
            gridBalance.DataSource = tBalance;
            FormatMoney(gridBalance);
            PutBalanceTotals(tBalance);

            // 3) Estado de resultados
            var tER = BuildResultadosTable(tLibro);
            gridResultados.DataSource = tER;
            FormatMoney(gridResultados);
            PutResultadosTotals(tER);
        }

        private void FormatLibroGrid()
        {
            if (gridLibro.Columns["Fecha"] != null)
                gridLibro.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
            if (gridLibro.Columns["Debe"] != null)
                gridLibro.Columns["Debe"].DefaultCellStyle.Format = "C2";
            if (gridLibro.Columns["Haber"] != null)
                gridLibro.Columns["Haber"].DefaultCellStyle.Format = "C2";
        }

        private void FormatMoney(DataGridView gv)
        {
            foreach (DataGridViewColumn c in gv.Columns)
            {
                if (c.ValueType == typeof(decimal)) c.DefaultCellStyle.Format = "C2";
            }
        }

        private void PutLibroTotals(DataTable tLibro)
        {
            decimal debe = tLibro.AsEnumerable().Sum(r => r.Field<decimal>("Debe"));
            decimal haber = tLibro.AsEnumerable().Sum(r => r.Field<decimal>("Haber"));
            lblLibroTotales.Text = $"Total Debe: {debe:C2}    Total Haber: {haber:C2}    Diferencia: {(debe - haber):C2}";
        }

        // ====== Balance ======
        private DataTable BuildBalanceTable(DataTable tLibro)
        {
            var t = new DataTable();
            t.Columns.Add("Tipo", typeof(string));
            t.Columns.Add("CodCuenta", typeof(string));
            t.Columns.Add("Cuenta", typeof(string));
            t.Columns.Add("Saldo", typeof(decimal));

            // saldo por cuenta (con signo según tipo)
            var porCuenta = tLibro.AsEnumerable()
                .GroupBy(r => new
                {
                    Tipo = r.Field<string>("TipoCuenta"),
                    Cod = r.Field<string>("CodCuenta"),
                    Name = r.Field<string>("Cuenta")
                })
                .Select(g =>
                {
                    string tipo = g.Key.Tipo;
                    decimal debe = g.Sum(r => r.Field<decimal>("Debe"));
                    decimal haber = g.Sum(r => r.Field<decimal>("Haber"));
                    decimal saldo = (tipo == "Activo" || tipo == "Gasto") ? (debe - haber) : (haber - debe);
                    return new { g.Key.Tipo, g.Key.Cod, g.Key.Name, Saldo = saldo };
                })
                .Where(x => x.Saldo != 0m)
                .OrderBy(x => x.Tipo).ThenBy(x => x.Cod);

            foreach (var x in porCuenta)
            {
                var r = t.NewRow();
                r["Tipo"] = x.Tipo;
                r["CodCuenta"] = x.Cod;
                r["Cuenta"] = x.Name;
                r["Saldo"] = x.Saldo;
                t.Rows.Add(r);
            }

            return t;
        }

        private void PutBalanceTotals(DataTable tBal)
        {
            decimal activos = SumByType(tBal, "Activo");
            decimal pasivos = SumByType(tBal, "Pasivo");
            decimal patr = SumByType(tBal, "Patrimonio");
            decimal ladoP = pasivos + patr;

            lblBalanceTotales.Text =
                $"Activos: {activos:C2}    Pasivo+Patrimonio: {ladoP:C2}    Diferencia: {(activos - ladoP):C2}";
        }

        private static decimal SumByType(DataTable t, string tipo) =>
            t.AsEnumerable()
             .Where(r => string.Equals(r.Field<string>("Tipo"), tipo, StringComparison.OrdinalIgnoreCase))
             .Sum(r => r.Field<decimal>("Saldo"));

        // ====== Estado de resultados ======
        private DataTable BuildResultadosTable(DataTable tLibro)
        {
            var t = new DataTable();
            t.Columns.Add("Concepto", typeof(string));
            t.Columns.Add("Monto", typeof(decimal));

            decimal ingresos = tLibro.AsEnumerable()
                .Where(r => r.Field<string>("TipoCuenta") == "Ingreso")
                .Sum(r => r.Field<decimal>("Haber") - r.Field<decimal>("Debe"));

            decimal gastos = tLibro.AsEnumerable()
                .Where(r => r.Field<string>("TipoCuenta") == "Gasto")
                .Sum(r => r.Field<decimal>("Debe") - r.Field<decimal>("Haber"));

            decimal utilidad = ingresos - gastos;

            t.Rows.Add("Ingresos", ingresos);
            t.Rows.Add("Gastos", gastos);
            t.Rows.Add("Utilidad", utilidad);

            return t;
        }

        private void PutResultadosTotals(DataTable tER)
        {
            decimal ingresos = Get(tER, "Ingresos");
            decimal gastos = Get(tER, "Gastos");
            decimal util = Get(tER, "Utilidad");
            lblResultados.Text = $"Ingresos: {ingresos:C2}    Gastos: {gastos:C2}    Utilidad: {util:C2}";

            static decimal Get(DataTable t, string name) =>
                t.AsEnumerable().First(r => r.Field<string>("Concepto") == name).Field<decimal>("Monto");
        }

        // ====== Acciones ======
        private void NuevoAsiento()
        {
            using var dlg = new NuevoAsientoDialog(_plan);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            // Debe
            if (dlg.Debe > 0)
                AddRow(dlg.Fecha, dlg.Codigo, dlg.Nombre, dlg.Tipo, dlg.Descripcion, dlg.Debe, 0m);
            // Haber
            if (dlg.Haber > 0)
                AddRow(dlg.Fecha, dlg.Codigo, dlg.Nombre, dlg.Tipo, dlg.Descripcion, 0m, dlg.Haber);

            ApplyFiltersAndRefreshAll();
        }

        private async Task ExportActiveTabToExcelAsync()
        {
            DataGridView gv = tab.SelectedTab == tabLibro ? gridLibro :
                              tab.SelectedTab == tabBalance ? gridBalance :
                              gridResultados;

            if (gv.DataSource is not DataTable dt || dt.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var columns = gv.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .Select(c => new ColumnExportInfo(
                    string.IsNullOrWhiteSpace(c.HeaderText) ? c.Name : c.HeaderText,
                    string.IsNullOrWhiteSpace(c.DataPropertyName) ? c.Name : c.DataPropertyName,
                    c.Name))
                .ToList();

            if (columns.Count == 0)
            {
                MessageBox.Show("No hay columnas visibles para exportar.", "Exportar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dataToExport = dt.Copy();
            string defaultName = BuildDefaultExportName(tab.SelectedTab?.Text);

            using var dlg = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = defaultName,
                AddExtension = true,
                DefaultExt = "xlsx"
            };

            while (true)
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string fileName = Path.ChangeExtension(dlg.FileName, ".xlsx");

                try
                {
                    await Task.Run(() => ExportDataTableToExcel(dataToExport, columns, fileName));
                    MessageBox.Show($"Archivo exportado correctamente: {Path.GetFileName(fileName)}", "Exportar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }
                catch (Exception ex)
                {
                    var retry = MessageBox.Show(
                        $"No se pudo exportar el archivo.{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}¿Desea elegir otra ubicación?",
                        "Error al exportar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (retry == DialogResult.Yes)
                    {
                        dlg.FileName = Path.GetFileName(fileName);
                        continue;
                    }

                    break;
                }
            }
        }

        private static string BuildDefaultExportName(string? tabName)
        {
            string baseName = new string((tabName ?? "Exportacion")
                .Where(ch => char.IsLetterOrDigit(ch))
                .ToArray());
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Exportacion";
            }

            return $"{baseName}_{DateTime.Now:yyyy-MM-dd}.xlsx";
        }

        private static void ExportDataTableToExcel(DataTable table, IReadOnlyList<ColumnExportInfo> columns, string filePath)
        {
            var culture = CultureInfo.CurrentCulture;
            string currencyFormat = $"{culture.NumberFormat.CurrencySymbol} #,##0.{new string('0', culture.NumberFormat.CurrencyDecimalDigits)}";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Exportación");

            // Encabezados
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = columns[i].HeaderText;
            }

            var headerRange = worksheet.Range(1, 1, 1, columns.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(234, 234, 234);
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Datos
            int rowIndex = 2;
            foreach (DataRow row in table.Rows)
            {
                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                {
                    var columnName = ResolveColumnName(table, columns[colIndex]);
                    var cell = worksheet.Cell(rowIndex, colIndex + 1);

                    if (columnName is null)
                    {
                        cell.Value = string.Empty;
                        continue;
                    }

                    var value = row[columnName];

                    // CORRECCIÓN: Conversión explícita según el tipo
                    if (value is DBNull || value == null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (value is DateTime dt)
                    {
                        cell.Value = dt;
                    }
                    else if (IsNumericType(value.GetType()))
                    {
                        cell.Value = Convert.ToDouble(value);
                    }
                    else
                    {
                        cell.Value = value.ToString() ?? string.Empty;
                    }
                }
                rowIndex++;
            }

            // Fila de totales
            int totalsRowIndex = rowIndex;
            var totalsRowRange = worksheet.Range(totalsRowIndex, 1, totalsRowIndex, columns.Count);
            totalsRowRange.Style.Font.Bold = true;
            totalsRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);

            var totals = new Dictionary<int, decimal>();
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var columnName = ResolveColumnName(table, columns[colIndex]);
                if (columnName is null)
                {
                    continue;
                }

                var dataType = table.Columns[columnName].DataType;
                if (!IsNumericType(dataType))
                {
                    continue;
                }

                decimal sum = 0m;
                foreach (DataRow row in table.Rows)
                {
                    if (row.IsNull(columnName)) continue;
                    sum += Convert.ToDecimal(row[columnName], CultureInfo.InvariantCulture);
                }

                totals[colIndex] = sum;
                worksheet.Cell(totalsRowIndex, colIndex + 1).Value = sum;
            }

            // Etiqueta de totales
            int labelColumnIndex = FindPreferredLabelColumn(columns, table);
            if (labelColumnIndex >= 0)
            {
                worksheet.Cell(totalsRowIndex, labelColumnIndex + 1).Value = "Totales";
            }

            // Diferencia Debe-Haber
            if (labelColumnIndex >= 0 &&
                TryGetColumnIndex(columns, table, "Debe", out int debeColumnIndex) &&
                TryGetColumnIndex(columns, table, "Haber", out int haberColumnIndex) &&
                totals.TryGetValue(debeColumnIndex, out var totalDebe) &&
                totals.TryGetValue(haberColumnIndex, out var totalHaber))
            {
                decimal diferencia = totalDebe - totalHaber;
                var differenceCell = worksheet.Cell(totalsRowIndex, labelColumnIndex + 1);
                string baseValue = differenceCell.GetString();
                string formattedDifference = diferencia.ToString(currencyFormat, culture);
                differenceCell.Value = string.IsNullOrWhiteSpace(baseValue)
                    ? $"Diferencia: {formattedDifference}"
                    : $"{baseValue} (Diferencia: {formattedDifference})";
            }

            // Bordes - CORRECCIÓN: Usar métodos compatibles con ClosedXML reciente
            var tableRange = worksheet.Range(1, 1, totalsRowIndex, columns.Count);
            tableRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            tableRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Hair);
            tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Formato de columnas
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var column = columns[colIndex];
                var columnName = ResolveColumnName(table, column);
                var worksheetColumn = worksheet.Column(colIndex + 1);

                if (columnName is null)
                {
                    worksheetColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    worksheetColumn.AdjustToContents();
                    continue;
                }

                var dataType = table.Columns[columnName].DataType;

                if (dataType == typeof(DateTime))
                {
                    worksheetColumn.Style.DateFormat.Format = "dd/MM/yyyy";
                    worksheetColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
                else if (IsNumericType(dataType))
                {
                    worksheetColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    if (string.Equals(columnName, "Debe", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(columnName, "Haber", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheetColumn.Style.NumberFormat.Format = currencyFormat;
                    }
                    else
                    {
                        worksheetColumn.Style.NumberFormat.Format = "#,##0.00";
                    }
                }
                else
                {
                    worksheetColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                worksheetColumn.AdjustToContents();
            }

            workbook.SaveAs(filePath);
        }

        private static string? ResolveColumnName(DataTable table, ColumnExportInfo column)
        {
            if (!string.IsNullOrWhiteSpace(column.DataPropertyName) && table.Columns.Contains(column.DataPropertyName))
            {
                return column.DataPropertyName;
            }

            if (!string.IsNullOrWhiteSpace(column.Name) && table.Columns.Contains(column.Name))
            {
                return column.Name;
            }

            return null;
        }

        private static int FindPreferredLabelColumn(IReadOnlyList<ColumnExportInfo> columns, DataTable table)
        {
            string[] preferred = new[] { "Descripción", "Descripcion", "Cuenta", "TipoCuenta" };
            foreach (string expected in preferred)
            {
                if (TryGetColumnIndex(columns, table, expected, out int index) && IsTextColumn(table, columns[index]))
                {
                    return index;
                }
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (IsTextColumn(table, columns[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryGetColumnIndex(IReadOnlyList<ColumnExportInfo> columns, DataTable table, string name, out int index)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (string.Equals(column.DataPropertyName, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(column.HeaderText, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static bool IsTextColumn(DataTable table, ColumnExportInfo column)
        {
            var columnName = ResolveColumnName(table, column);
            return columnName is not null && table.Columns[columnName].DataType == typeof(string);
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        private sealed record ColumnExportInfo(string HeaderText, string DataPropertyName, string Name);

        }
    }

    // ====== Utilidades ======
    internal static class DtExt
    {
        public static DataTable CopyToDataTableOrEmpty(this IOrderedEnumerable<DataRow> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0) return new DataTable();
            return list.CopyToDataTable();
        }
    }

    // ====== Diálogo para crear líneas de asiento (simple) ======
    internal class NuevoAsientoDialog : Form
    {
        private readonly (string Codigo, string Nombre, string Tipo)[] _plan;
        private ComboBox cmbCuenta;
        private DateTimePicker dt;
        private TextBox txtDesc;
        private NumericUpDown numDebe, numHaber;
        private Button btnOk, btnCancel;

        public DateTime Fecha => dt.Value.Date;
        public string Codigo { get; private set; } = "";
        public string Nombre { get; private set; } = "";
        public string Tipo { get; private set; } = "";
        public string Descripcion => txtDesc.Text.Trim();
        public decimal Debe => numDebe.Value;
        public decimal Haber => numHaber.Value;

        public NuevoAsientoDialog((string Codigo, string Nombre, string Tipo)[] plan)
        {
            _plan = plan;
            Text = "Nuevo asiento (línea)";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(540, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var lblC = new Label { Text = "Cuenta:", Left = 16, Top = 20, AutoSize = true };
            cmbCuenta = new ComboBox { Left = 80, Top = 16, Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCuenta.Items.AddRange(_plan.Select(p => $"{p.Codigo} - {p.Nombre} ({p.Tipo})").ToArray());
            if (cmbCuenta.Items.Count > 0) cmbCuenta.SelectedIndex = 0;

            var lblF = new Label { Text = "Fecha:", Left = 16, Top = 54, AutoSize = true };
            dt = new DateTimePicker { Left = 80, Top = 50, Width = 120, Format = DateTimePickerFormat.Short };

            var lblD = new Label { Text = "Descripción:", Left = 210, Top = 54, AutoSize = true };
            txtDesc = new TextBox { Left = 294, Top = 50, Width = 216 };

            var lblDebe = new Label { Text = "Debe:", Left = 16, Top = 90, AutoSize = true };
            numDebe = new NumericUpDown { Left = 80, Top = 86, Width = 120, DecimalPlaces = 2, Maximum = 1000000000, Minimum = 0 };
            var lblHaber = new Label { Text = "Haber:", Left = 210, Top = 90, AutoSize = true };
            numHaber = new NumericUpDown { Left = 264, Top = 86, Width = 120, DecimalPlaces = 2, Maximum = 1000000000, Minimum = 0 };

            btnOk = new Button { Text = "Agregar", Left = 340, Top = 140, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancelar", Left = 430, Top = 140, Width = 80, DialogResult = DialogResult.Cancel };
            AcceptButton = btnOk; CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblC, cmbCuenta, lblF, dt, lblD, txtDesc, lblDebe, numDebe, lblHaber, numHaber, btnOk, btnCancel });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (numDebe.Value <= 0 && numHaber.Value <= 0)
                {
                    MessageBox.Show("Indica un valor en Debe o Haber.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true; return;
                }

                // Tomar cuenta seleccionada
                var sel = cmbCuenta.SelectedItem?.ToString() ?? "";
                var cod = sel.Split('-')[0].Trim();
                var hit = _plan.FirstOrDefault(p => p.Codigo == cod);
                if (string.IsNullOrEmpty(hit.Codigo))
                {
                    MessageBox.Show("Cuenta inválida.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true; return;
                }

                Codigo = hit.Codigo;
                Nombre = hit.Nombre;
                Tipo = hit.Tipo;
            }
            base.OnFormClosing(e);
        }
    }

