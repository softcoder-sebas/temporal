using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsSortOrder = System.Windows.Forms.SortOrder;

namespace MyMarket_ERP
{
    public partial class Clientes : Form
    {
        private readonly BindingList<Customer> _rows = new();
        private readonly BindingSource _bs = new();
        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource? _reloadCts;
        private bool _isLoading;
        private int? _lastKnownSelectionId;

        public Clientes()
        {
            InitializeComponent();

            // === Sidebar / Navegación ===
            var role = AppSession.Role;
            this.Tag = NavSection.Clientes;

            SidebarInstaller.Install(
                this,
                role,
                NavSection.Clientes,
                section => NavigationService.Open(section, this, role)
            );

            // === UI de la grilla ===
            GridSetup();

            // === Eventos ===
            btnNuevo.Click += (_, __) => AbrirNuevoCliente();
            btnEditar.Click += (_, __) => EditarSeleccionado();
            btnEliminar.Click += (_, __) => EliminarSeleccionado();

            txtBuscar.TextChanged += (_, __) => AplicarFiltro();
            gridClientes.SelectionChanged += (_, __) =>
            {
                RememberSelection();
                UpdateActionStates();
            };
            Shown += (_, __) => txtBuscar.Focus();
            Shown += async (_, __) => await RefreshClientesAsync();
            FormClosed += (_, __) => DisposeSubscriptions();

            _subscriptions.Add(DataEvents.SubscribeClientes(this, payload => _ = RefreshClientesAsync(payload.EntityId)));

            // === BD ===
            UpdateActionStates();
        }

        // ========== BD ==========
        private async Task RefreshClientesAsync(int? focusId = null)
        {
            var selectedId = focusId ?? _lastKnownSelectionId ?? GetSelectedCustomerId();
            var sortedColumn = gridClientes.SortedColumn?.DataPropertyName;
            var sortOrder = gridClientes.SortOrder;

            SetLoadingState(true);

            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            var cts = new CancellationTokenSource();
            _reloadCts = cts;

            List<Customer> data;
            try
            {
                data = await Task.Run(() => FetchCustomers(cts.Token), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando clientes:\n" + ex.Message,
                    "SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                if (_reloadCts == cts)
                {
                    _reloadCts = null;
                }
            }

            if (cts.IsCancellationRequested || IsDisposed)
            {
                return;
            }

            _rows.Clear();
            foreach (var customer in data)
            {
                _rows.Add(customer);
            }

            AplicarFiltro(selectedId);
            RestoreSort(sortedColumn, sortOrder);
            SetLoadingState(false);
        }

        private static List<Customer> FetchCustomers(CancellationToken token)
        {
            var result = new List<Customer>();
            using var cn = Database.OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT c.Id, c.Name, c.Email, c.Document, c.Phone, c.Address,
                       ISNULL(SUM(i.Total), 0) AS TotalSpent,
                       ISNULL(COUNT(i.Id), 0) AS PurchaseCount
                FROM dbo.Customers c
                LEFT JOIN dbo.Invoices i ON i.CustomerId = c.Id
                GROUP BY c.Id, c.Name, c.Email, c.Document, c.Phone, c.Address
                ORDER BY c.Id DESC;", cn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                token.ThrowIfCancellationRequested();
                result.Add(new Customer
                {
                    Id = rd.GetInt32(0),
                    Name = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    Email = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    Document = rd.IsDBNull(3) ? "" : rd.GetString(3),
                    Phone = rd.IsDBNull(4) ? "" : rd.GetString(4),
                    Address = rd.IsDBNull(5) ? "" : rd.GetString(5),
                    TotalSpent = rd.IsDBNull(6) ? 0m : rd.GetDecimal(6),
                    PurchaseCount = rd.IsDBNull(7) ? 0 : rd.GetInt32(7)
                });
            }

            AssignCustomerSegments(result);
            return result;
        }

        private static void AssignCustomerSegments(List<Customer> customers)
        {
            if (customers.Count == 0)
            {
                return;
            }

            decimal maxSpent = customers.Max(c => c.TotalSpent);
            int maxPurchases = customers.Max(c => c.PurchaseCount);

            foreach (var customer in customers)
            {
                decimal spendScore = maxSpent > 0 ? customer.TotalSpent / maxSpent : 0m;
                decimal frequencyScore = maxPurchases > 0 ? (decimal)customer.PurchaseCount / maxPurchases : 0m;
                decimal combinedScore = (spendScore * 0.6m) + (frequencyScore * 0.4m);

                customer.ValueScore = combinedScore;

                if (combinedScore >= 0.7m)
                {
                    customer.Segment = "A";
                }
                else if (combinedScore >= 0.4m)
                {
                    customer.Segment = "B";
                }
                else
                {
                    customer.Segment = "C";
                }
            }
        }

        // ========== UI ==========
        private void GridSetup()
        {
            gridClientes.AutoGenerateColumns = false;
            gridClientes.ReadOnly = true;
            gridClientes.RowHeadersVisible = false;
            gridClientes.AllowUserToAddRows = false;
            gridClientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridClientes.MultiSelect = false;
            ModernTheme.StyleDataGrid(gridClientes);

            if (gridClientes.Columns.Count == 0)
            {
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = nameof(Customer.Id), Width = 60 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = nameof(Customer.Name), Width = 180 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Correo", DataPropertyName = nameof(Customer.Email), Width = 180 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cédula", DataPropertyName = nameof(Customer.Document), Width = 120 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Teléfono", DataPropertyName = nameof(Customer.Phone), Width = 120 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dirección", DataPropertyName = nameof(Customer.Address), Width = 200 });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Compras",
                    DataPropertyName = nameof(Customer.PurchaseCount),
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Total comprado",
                    DataPropertyName = nameof(Customer.TotalSpent),
                    Width = 130,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Format = "C0",
                        Alignment = DataGridViewContentAlignment.MiddleRight
                    }
                });
                gridClientes.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Segmento",
                    DataPropertyName = nameof(Customer.Segment),
                    Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    }
                });
            }

            _bs.DataSource = _rows;
            gridClientes.DataSource = _bs;
        }

        private void AplicarFiltro(int? selectId = null)
        {
            string q = (txtBuscar.Text ?? "").Trim().ToLowerInvariant();
            IEnumerable<Customer> data = _rows;

            if (!string.IsNullOrEmpty(q))
            {
                data = _rows.Where(c =>
                    (c.Name ?? "").ToLowerInvariant().Contains(q) ||
                    (c.Email ?? "").ToLowerInvariant().Contains(q) ||
                    (c.Document ?? "").ToLowerInvariant().Contains(q) ||
                    (c.Phone ?? "").ToLowerInvariant().Contains(q) ||
                    (c.Address ?? "").ToLowerInvariant().Contains(q) ||
                    (c.Segment ?? "").ToLowerInvariant().Contains(q)
                );
            }

            var filtered = data.ToList();

            _bs.DataSource = new BindingList<Customer>(filtered);
            gridClientes.DataSource = _bs;
            UpdateStatusLabel(filtered.Count);

            int? targetId = selectId ?? _lastKnownSelectionId;
            if (targetId.HasValue)
            {
                SelectCustomerInGrid(targetId.Value);
            }
            else
            {
                gridClientes.ClearSelection();
                gridClientes.CurrentCell = null;
            }

            UpdateActionStates();
        }

        private void SelectCustomerInGrid(int customerId)
        {
            foreach (DataGridViewRow row in gridClientes.Rows)
            {
                if (row.DataBoundItem is Customer c && c.Id == customerId)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0)
                    {
                        gridClientes.CurrentCell = row.Cells[0];
                    }

                    if (row.Index >= 0)
                    {
                        gridClientes.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index - 2);
                    }
                    _lastKnownSelectionId = customerId;
                    break;
                }
            }
        }

        private bool _customerDialogOpen;

        private void AbrirNuevoCliente()
        {
            AbrirDialogoCliente(null);
        }

        private void EditarSeleccionado()
        {
            if (gridClientes.CurrentRow?.DataBoundItem is not Customer c)
            {
                MessageBox.Show("Selecciona un cliente.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AbrirDialogoCliente(c);
        }

        private void AbrirDialogoCliente(Customer? existing)
        {
            if (_customerDialogOpen)
            {
                return;
            }

            _customerDialogOpen = true;
            btnNuevo.Enabled = false;
            btnEditar.Enabled = false;

            try
            {
                using var dialog = new ClienteDialog();
                if (existing is null)
                {
                    dialog.ConfigureForCreate();
                }
                else
                {
                    dialog.ConfigureForEdit(existing);
                }

                var result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    var data = dialog.GetData();

                    if (existing is null)
                    {
                        if (CrearCliente(data, out int newId))
                        {
                            MessageBox.Show("Cliente guardado.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DataEvents.PublishClientesChanged(newId);
                        }
                    }
                    else
                    {
                        if (ActualizarCliente(existing.Id, data))
                        {
                            MessageBox.Show("Cliente actualizado.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DataEvents.PublishClientesChanged(existing.Id);
                        }
                    }
                }
            }
            finally
            {
                _customerDialogOpen = false;
                UpdateActionStates();
            }
        }

        private bool CrearCliente(CustomerFormData data, out int newCustomerId)
        {
            newCustomerId = 0;

            try
            {
                using var cn = Database.OpenConnection();
                using var tx = cn.BeginTransaction();

                using (var cmd = new SqlCommand(@"
                INSERT INTO dbo.Customers(Name,Email,Document,Phone,Address)
                VALUES(@n,@e,@d,@p,@a);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@n", data.Name);
                    cmd.Parameters.AddWithValue("@e", data.Email);
                    cmd.Parameters.AddWithValue("@d", data.Document);
                    cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(data.Phone) ? (object)DBNull.Value : data.Phone);
                    cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(data.Address) ? (object)DBNull.Value : data.Address);
                    newCustomerId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                EnsureCustomerUser(cn, tx, newCustomerId, data.Email, data.Document);

                tx.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("El correo o la cédula ya existe en el sistema.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando cliente:\n" + ex.Message, "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private bool ActualizarCliente(int customerId, CustomerFormData data)
        {
            try
            {
                using var cn = Database.OpenConnection();
                using var tx = cn.BeginTransaction();

                using (var cmd = new SqlCommand(@"
                    UPDATE dbo.Customers
                       SET Name=@n, Email=@e, Document=@d, Phone=@p, Address=@a
                     WHERE Id=@id;", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@n", data.Name);
                    cmd.Parameters.AddWithValue("@e", data.Email);
                    cmd.Parameters.AddWithValue("@d", data.Document);
                    cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(data.Phone) ? (object)DBNull.Value : data.Phone);
                    cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(data.Address) ? (object)DBNull.Value : data.Address);
                    cmd.Parameters.AddWithValue("@id", customerId);
                    cmd.ExecuteNonQuery();
                }

                EnsureCustomerUser(cn, tx, customerId, data.Email, data.Document);

                tx.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Ya existe otro cliente con esa cédula o correo.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editando cliente:\n" + ex.Message, "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private void EliminarSeleccionado()
        {
            if (gridClientes.CurrentRow?.DataBoundItem is not Customer c)
            { MessageBox.Show("Selecciona un cliente.", "Clientes"); return; }

            if (MessageBox.Show($"¿Eliminar a \"{c.Name}\"?", "Clientes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var cn = Database.OpenConnection();
                using var tx = cn.BeginTransaction();

                using (var delUser = new SqlCommand("DELETE FROM dbo.Users WHERE CustomerId=@id;", cn, tx))
                {
                    delUser.Parameters.AddWithValue("@id", c.Id);
                    delUser.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("DELETE FROM dbo.Customers WHERE Id=@id;", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", c.Id);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("No se puede eliminar el cliente porque tiene movimientos relacionados.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error eliminando cliente:\n" + ex.Message, "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataEvents.PublishClientesChanged();
        }

        private void RestoreSort(string? columnName, WinFormsSortOrder sortOrder)
        {
            if (string.IsNullOrWhiteSpace(columnName) || sortOrder == WinFormsSortOrder.None)
            {
                return;
            }

            try
            {
                var column = gridClientes.Columns
                    .Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => string.Equals(c.DataPropertyName, columnName, StringComparison.OrdinalIgnoreCase));

                if (column != null)
                {
                    var direction = sortOrder == WinFormsSortOrder.Descending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;

                    gridClientes.Sort(column, direction);
                }
            }
            catch (NotSupportedException)
            {
                // Origen sin soporte de ordenamiento.
            }
        }

        private void RememberSelection()
        {
            _lastKnownSelectionId = GetSelectedCustomerId();
        }

        private int? GetSelectedCustomerId()
        {
            return gridClientes.CurrentRow?.DataBoundItem is Customer c ? c.Id : null;
        }

        private void UpdateActionStates()
        {
            bool hasSelection = gridClientes.CurrentRow?.DataBoundItem is Customer;
            bool dialogOpen = _customerDialogOpen;

            btnNuevo.Enabled = !_isLoading && !dialogOpen;
            btnEditar.Enabled = !_isLoading && hasSelection && !dialogOpen;
            btnEliminar.Enabled = !_isLoading && hasSelection;
        }

        private void SetLoadingState(bool loading)
        {
            _isLoading = loading;
            UpdateStatusLabel(_bs.List?.Count ?? _rows.Count);
            UpdateActionStates();
        }

        private void UpdateStatusLabel(int count)
        {
            if (_isLoading)
            {
                lblStatus.Text = "Actualizando clientes…";
                lblSegmentDescription.Text = "Calculando métricas de clientes…";
                return;
            }

            IEnumerable<Customer> current = _bs.List is IEnumerable list
                ? list.Cast<Customer>()
                : _rows;

            var segmentSummary = current
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Segment) ? "Sin segmento" : c.Segment)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}");

            string suffix = string.Join(" • ", segmentSummary);
            lblStatus.Text = string.IsNullOrEmpty(suffix)
                ? $"Clientes: {count}"
                : $"Clientes: {count} ({suffix})";

            var metricsA = CalculateSegmentMetrics(current, "A");
            var metricsB = CalculateSegmentMetrics(current, "B");
            var metricsC = CalculateSegmentMetrics(current, "C");

            lblSegmentDescription.Text = string.Join(Environment.NewLine, new[]
            {
                FormatSegmentMetrics("Segmento A (máximo)", metricsA, SegmentMetricMode.Maximum),
                FormatSegmentMetrics("Segmento B (intermedio)", metricsB, SegmentMetricMode.Average),
                FormatSegmentMetrics("Segmento C (mínimo)", metricsC, SegmentMetricMode.Minimum)
            });
        }

        private static SegmentMetrics CalculateSegmentMetrics(IEnumerable<Customer> customers, string segmentKey)
        {
            var segmentCustomers = customers
                .Where(c => string.Equals(c.Segment, segmentKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (segmentCustomers.Count == 0)
            {
                return SegmentMetrics.Empty;
            }

            int minPurchases = segmentCustomers.Min(c => c.PurchaseCount);
            int maxPurchases = segmentCustomers.Max(c => c.PurchaseCount);
            decimal minTotal = segmentCustomers.Min(c => c.TotalSpent);
            decimal maxTotal = segmentCustomers.Max(c => c.TotalSpent);
            decimal avgPurchases = (decimal)segmentCustomers.Average(c => c.PurchaseCount);
            decimal avgTotal = segmentCustomers.Average(c => c.TotalSpent);
            int totalPurchases = segmentCustomers.Sum(c => c.PurchaseCount);
            decimal totalSpent = segmentCustomers.Sum(c => c.TotalSpent);
            decimal avgTicket = totalPurchases > 0 ? totalSpent / totalPurchases : 0m;

            return new SegmentMetrics(
                segmentCustomers.Count,
                minPurchases,
                maxPurchases,
                minTotal,
                maxTotal,
                avgPurchases,
                avgTotal,
                avgTicket
            );
        }

        private static string FormatSegmentMetrics(string label, SegmentMetrics metrics, SegmentMetricMode mode)
        {
            if (!metrics.HasCustomers)
            {
                return $"{label}: sin clientes en este segmento.";
            }

            string purchasesText = mode switch
            {
                SegmentMetricMode.Minimum => $"{metrics.MinPurchases} compras mínimas",
                SegmentMetricMode.Average => $"{FormatNumber(metrics.AveragePurchases)} compras promedio",
                SegmentMetricMode.Maximum => $"{metrics.MaxPurchases} compras máximas",
                _ => string.Empty
            };

            string totalText = mode switch
            {
                SegmentMetricMode.Minimum => $"total mínimo {FormatCurrency(metrics.MinTotalSpent)}",
                SegmentMetricMode.Average => $"total promedio {FormatCurrency(metrics.AverageTotalSpent)}",
                SegmentMetricMode.Maximum => $"total máximo {FormatCurrency(metrics.MaxTotalSpent)}",
                _ => string.Empty
            };

            string ticketText = metrics.AverageTicket > 0m
                ? $"ticket promedio {FormatCurrency(metrics.AverageTicket)}"
                : "sin compras registradas";

            return $"{label}: {purchasesText} • {totalText} • {ticketText}";
        }

        private static string FormatCurrency(decimal amount)
        {
            return amount.ToString("C0");
        }

        private static string FormatNumber(decimal value)
        {
            return value.ToString("0.##");
        }

        private readonly struct SegmentMetrics
        {
            public static SegmentMetrics Empty { get; } = new();

            public SegmentMetrics(
                int customerCount,
                int minPurchases,
                int maxPurchases,
                decimal minTotalSpent,
                decimal maxTotalSpent,
                decimal averagePurchases,
                decimal averageTotalSpent,
                decimal averageTicket)
            {
                CustomerCount = customerCount;
                MinPurchases = minPurchases;
                MaxPurchases = maxPurchases;
                MinTotalSpent = minTotalSpent;
                MaxTotalSpent = maxTotalSpent;
                AveragePurchases = averagePurchases;
                AverageTotalSpent = averageTotalSpent;
                AverageTicket = averageTicket;
            }

            public int CustomerCount { get; }
            public int MinPurchases { get; }
            public int MaxPurchases { get; }
            public decimal MinTotalSpent { get; }
            public decimal MaxTotalSpent { get; }
            public decimal AveragePurchases { get; }
            public decimal AverageTotalSpent { get; }
            public decimal AverageTicket { get; }

            public bool HasCustomers => CustomerCount > 0;
        }

        private enum SegmentMetricMode
        {
            Minimum,
            Average,
            Maximum
        }

        private void DisposeSubscriptions()
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }

        private sealed class CustomerFormData
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Document { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }

        private sealed class ClienteDialog : Form
        {
            private readonly TextBox txtNombre;
            private readonly TextBox txtCorreo;
            private readonly TextBox txtCedula;
            private readonly TextBox txtTelefono;
            private readonly TextBox txtDireccion;
            private readonly Button btnGuardar;
            private readonly Button btnCancelar;
            private readonly Label lblError;
            private readonly ToolTip toolTip;

            private bool _showValidationMessage;
            private CustomerFormData _data = new();

            public ClienteDialog()
            {
                AutoScaleDimensions = new SizeF(8F, 20F);
                AutoScaleMode = AutoScaleMode.Dpi;
                Font = ModernTheme.Body;
                ForeColor = ModernTheme.TextPrimary;
                BackColor = ModernTheme.Surface;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                Padding = new Padding(24);
                Text = "Cliente";

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 7,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                Label CreateLabel(string text) => new Label
                {
                    Text = text,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = ModernTheme.TextSecondary,
                    Padding = new Padding(0, 6, 12, 6),
                    Dock = DockStyle.Fill
                };

                TextBox CreateTextBox(int width, bool multiline = false)
                {
                    var tb = new TextBox
                    {
                        Width = width,
                        Dock = DockStyle.Fill,
                        Multiline = multiline
                    };

                    if (multiline)
                    {
                        tb.Height = 80;
                        tb.ScrollBars = ScrollBars.Vertical;
                    }

                    return tb;
                }

                txtNombre = CreateTextBox(320);
                txtNombre.AccessibleName = "Nombre completo";
                txtNombre.TabIndex = 0;

                txtCorreo = CreateTextBox(320);
                txtCorreo.AccessibleName = "Correo electrónico";
                txtCorreo.TabIndex = 1;

                txtCedula = CreateTextBox(200);
                txtCedula.AccessibleName = "Documento de identidad";
                txtCedula.TabIndex = 2;

                txtTelefono = CreateTextBox(200);
                txtTelefono.AccessibleName = "Teléfono";
                txtTelefono.TabIndex = 3;

                txtDireccion = CreateTextBox(320, multiline: true);
                txtDireccion.AccessibleName = "Dirección";
                txtDireccion.TabIndex = 4;

                layout.Controls.Add(CreateLabel("Nombre completo *"), 0, 0);
                layout.Controls.Add(txtNombre, 1, 0);
                layout.Controls.Add(CreateLabel("Correo electrónico *"), 0, 1);
                layout.Controls.Add(txtCorreo, 1, 1);
                layout.Controls.Add(CreateLabel("Documento *"), 0, 2);
                layout.Controls.Add(txtCedula, 1, 2);
                layout.Controls.Add(CreateLabel("Teléfono"), 0, 3);
                layout.Controls.Add(txtTelefono, 1, 3);
                layout.Controls.Add(CreateLabel("Dirección"), 0, 4);
                layout.Controls.Add(txtDireccion, 1, 4);

                lblError = new Label
                {
                    ForeColor = Color.Firebrick,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Visible = false,
                    Margin = new Padding(0, 12, 0, 0)
                };
                layout.Controls.Add(lblError, 0, 5);
                layout.SetColumnSpan(lblError, 2);

                btnGuardar = ModernTheme.CreatePrimaryButton("Guardar");
                btnGuardar.TabIndex = 6;
                btnGuardar.Enabled = false;
                btnGuardar.AccessibleName = "Guardar cliente";
                btnGuardar.Click += (_, __) => TrySave();

                btnCancelar = ModernTheme.CreateGhostButton("Cancelar");
                btnCancelar.TabIndex = 7;
                btnCancelar.AccessibleName = "Cancelar";
                btnCancelar.DialogResult = DialogResult.Cancel;
                btnCancelar.Click += (_, __) => Close();

                var buttonsPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 16, 0, 0)
                };
                buttonsPanel.Controls.Add(btnGuardar);
                buttonsPanel.Controls.Add(btnCancelar);
                layout.Controls.Add(buttonsPanel, 0, 6);
                layout.SetColumnSpan(buttonsPanel, 2);

                toolTip = new ToolTip();
                toolTip.SetToolTip(txtNombre, "Nombre completo del cliente");
                toolTip.SetToolTip(txtCorreo, "Correo electrónico del cliente");
                toolTip.SetToolTip(txtCedula, "Documento de identidad");
                toolTip.SetToolTip(txtTelefono, "Teléfono de contacto");
                toolTip.SetToolTip(txtDireccion, "Dirección del cliente");

                Controls.Add(layout);

                AcceptButton = btnGuardar;
                CancelButton = btnCancelar;

                txtNombre.TextChanged += (_, __) => RefreshValidation();
                txtCorreo.TextChanged += (_, __) => RefreshValidation();
                txtCedula.TextChanged += (_, __) => RefreshValidation();
                txtTelefono.TextChanged += (_, __) => RefreshValidation();
                txtDireccion.TextChanged += (_, __) => RefreshValidation();
            }

            public void ConfigureForCreate()
            {
                Text = "Nuevo cliente";
                _showValidationMessage = false;
                txtNombre.Clear();
                txtCorreo.Clear();
                txtCedula.Clear();
                txtTelefono.Clear();
                txtDireccion.Clear();
                RefreshValidation();
                ActiveControl = txtNombre;
            }

            public void ConfigureForEdit(Customer customer)
            {
                Text = "Editar cliente";
                _showValidationMessage = true;
                txtNombre.Text = customer.Name ?? string.Empty;
                txtCorreo.Text = customer.Email ?? string.Empty;
                txtCedula.Text = customer.Document ?? string.Empty;
                txtTelefono.Text = customer.Phone ?? string.Empty;
                txtDireccion.Text = customer.Address ?? string.Empty;
                RefreshValidation();
                txtNombre.Focus();
                txtNombre.SelectAll();
            }

            public CustomerFormData GetData() => _data;

            private void RefreshValidation()
            {
                if (TryBuildData(out var data, out var message))
                {
                    _data = data;
                    btnGuardar.Enabled = true;
                    if (_showValidationMessage)
                    {
                        lblError.Visible = false;
                    }
                }
                else
                {
                    btnGuardar.Enabled = false;
                    if (_showValidationMessage)
                    {
                        lblError.Text = message;
                        lblError.Visible = true;
                    }
                    else
                    {
                        lblError.Visible = false;
                    }
                }
            }

            private void TrySave()
            {
                if (TryBuildData(out var data, out var message))
                {
                    _data = data;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _showValidationMessage = true;
                    lblError.Text = message;
                    lblError.Visible = true;
                    btnGuardar.Enabled = false;
                }
            }

            private bool TryBuildData(out CustomerFormData data, out string message)
            {
                string nombre = txtNombre.Text.Trim();
                string correo = txtCorreo.Text.Trim();
                string documento = txtCedula.Text.Trim();
                string telefono = txtTelefono.Text.Trim();
                string direccion = txtDireccion.Text.Trim();

                if (string.IsNullOrEmpty(nombre))
                {
                    message = "El nombre es obligatorio.";
                    data = new CustomerFormData();
                    return false;
                }

                if (nombre.Length > 120)
                {
                    message = "El nombre no puede superar 120 caracteres.";
                    data = new CustomerFormData();
                    return false;
                }

                if (string.IsNullOrEmpty(correo))
                {
                    message = "El correo es obligatorio.";
                    data = new CustomerFormData();
                    return false;
                }

                if (correo.Length > 100)
                {
                    message = "El correo no puede superar 100 caracteres.";
                    data = new CustomerFormData();
                    return false;
                }

                var rx = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!rx.IsMatch(correo))
                {
                    message = "Formato de correo inválido.";
                    data = new CustomerFormData();
                    return false;
                }

                if (string.IsNullOrEmpty(documento))
                {
                    message = "El documento es obligatorio.";
                    data = new CustomerFormData();
                    return false;
                }

                if (documento.Length > 40)
                {
                    message = "El documento no puede superar 40 caracteres.";
                    data = new CustomerFormData();
                    return false;
                }

                if (!string.IsNullOrEmpty(telefono) && telefono.Length > 30)
                {
                    message = "El teléfono no puede superar 30 caracteres.";
                    data = new CustomerFormData();
                    return false;
                }

                if (!string.IsNullOrEmpty(direccion) && direccion.Length > 200)
                {
                    message = "La dirección no puede superar 200 caracteres.";
                    data = new CustomerFormData();
                    return false;
                }

                data = new CustomerFormData
                {
                    Name = nombre,
                    Email = correo,
                    Document = documento,
                    Phone = telefono,
                    Address = direccion
                };
                message = string.Empty;
                return true;
            }
        }

        private static void EnsureCustomerUser(SqlConnection cn, SqlTransaction tx, int customerId, string email, string cedula)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(cedula))
                throw new ArgumentException("El correo y la cédula son obligatorios para crear credenciales de cliente.");

            string hashed = PasswordHasher.Hash(cedula);

            using var cmd = new SqlCommand(@"
                MERGE dbo.Users AS target
                USING (SELECT @CustomerId AS CustomerId) AS src
                ON target.CustomerId = src.CustomerId
                WHEN MATCHED THEN
                    UPDATE SET Email=@Email, Password=@Password, Role='cliente', IsActive=1
                WHEN NOT MATCHED THEN
                    INSERT (Email, Password, Role, IsActive, CustomerId)
                    VALUES (@Email, @Password, 'cliente', 1, @CustomerId);
            ", cn, tx);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", hashed);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.ExecuteNonQuery();
        }
    }

    // ===== Modelo con métricas de valor =====
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Document { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string Segment { get; set; } = string.Empty;
        public decimal ValueScore { get; set; }
    }
}
