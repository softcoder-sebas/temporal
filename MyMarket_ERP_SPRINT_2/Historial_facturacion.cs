using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class Historial_facturacion : Form
    {
        private readonly BindingList<InvoiceRow> _invoices = new();
        private readonly BindingSource _bsInvoices = new();
        private readonly BindingList<InvoiceItemRow> _items = new();
        private readonly BindingSource _bsItems = new();
        private readonly BindingList<CustomerOption> _customers = new();
        private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 350 };
        private bool _loadingCustomers;
        private bool _isAdmin;

        public Historial_facturacion()
        {
            InitializeComponent();

            _isAdmin = string.Equals(AppSession.Role, "admin", StringComparison.OrdinalIgnoreCase);
            this.Tag = NavSection.Historial;

            SidebarInstaller.Install(
                this,
                AppSession.Role,
                NavSection.Historial,
                section => NavigationService.Open(section, this, AppSession.Role)
            );

            SetupGrids();
            ConfigureFilters();

            btnRefrescar.Click += (_, __) => LoadInvoices();
            cboEstado.SelectedIndexChanged += (_, __) => LoadInvoices();
            cboMetodoPago.SelectedIndexChanged += (_, __) => LoadInvoices();
            dtDesde.ValueChanged += (_, __) => LoadInvoices();
            dtHasta.ValueChanged += (_, __) => LoadInvoices();
            gridFacturas.SelectionChanged += (_, __) => LoadInvoiceDetails();
            cboCliente.SelectedIndexChanged += (_, __) =>
            {
                if (!_loadingCustomers) LoadInvoices();
            };

            _searchTimer.Tick += (_, __) =>
            {
                _searchTimer.Stop();
                LoadInvoices();
            };

            txtBuscar.TextChanged += (_, __) =>
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };

            Load += Historial_facturacion_Load;
        }

        private void Historial_facturacion_Load(object? sender, EventArgs e)
        {
            // Configurar el SplitContainer cuando ya tiene su tamaño final
            splitMain.Panel1MinSize = 420;
            splitMain.Panel2MinSize = 360;

            // Establecer el SplitterDistance de manera proporcional
            int availableWidth = splitMain.Width - splitMain.SplitterWidth;
            int desiredDistance = (int)(availableWidth * 0.6); // 60% para facturas, 40% para detalle

            // Asegurar que respete los mínimos
            if (desiredDistance >= splitMain.Panel1MinSize &&
                (availableWidth - desiredDistance) >= splitMain.Panel2MinSize)
            {
                splitMain.SplitterDistance = desiredDistance;
            }

            dtDesde.Value = DateTime.Today.AddMonths(-1);
            dtHasta.Value = DateTime.Today;

            if (_isAdmin)
            {
                lblClienteActual.Visible = false;
                lblClienteSelector.Visible = true;
                cboCliente.Visible = true;
                LoadCustomerList();
            }
            else
            {
                lblClienteSelector.Visible = false;
                cboCliente.Visible = false;
                lblClienteActual.Visible = false;  // ← CAMBIO: Ocultar el label
                lblClienteResumenTitulo.Text = "Correo";
            }

            LoadInvoices();
        }

        private void ConfigureFilters()
        {
            cboEstado.Items.Clear();
            cboEstado.Items.Add("Todos");
            cboEstado.Items.AddRange(new object[] { "Pagada", "Pendiente", "Anulada" });
            cboEstado.SelectedIndex = 0;

            cboMetodoPago.Items.Clear();
            cboMetodoPago.Items.Add("Todos");
            cboMetodoPago.Items.AddRange(new object[] { "Efectivo", "Tarjeta", "Transferencia" });
            cboMetodoPago.SelectedIndex = 0;
        }

        private void SetupGrids()
        {
            gridFacturas.AutoGenerateColumns = false;
            gridFacturas.ReadOnly = true;
            gridFacturas.AllowUserToAddRows = false;
            gridFacturas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFacturas.MultiSelect = false;
            gridFacturas.RowHeadersVisible = false;
            gridFacturas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ModernTheme.StyleDataGrid(gridFacturas);

            if (gridFacturas.Columns.Count == 0)
            {
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Factura",
                    DataPropertyName = nameof(InvoiceRow.Number),
                    Width = 120,
                    FillWeight = 120
                });
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Fecha",
                    DataPropertyName = nameof(InvoiceRow.IssuedAt),
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
                    FillWeight = 160
                });
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Cliente",
                    DataPropertyName = nameof(InvoiceRow.CustomerName),
                    FillWeight = 180
                });
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Método",
                    DataPropertyName = nameof(InvoiceRow.PaymentMethod),
                    FillWeight = 110
                });
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Estado",
                    DataPropertyName = nameof(InvoiceRow.PaymentStatus),
                    FillWeight = 110
                });
                gridFacturas.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Total",
                    DataPropertyName = nameof(InvoiceRow.Total),
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
                    FillWeight = 120
                });
            }

            gridDetalle.AutoGenerateColumns = false;
            gridDetalle.ReadOnly = true;
            gridDetalle.AllowUserToAddRows = false;
            gridDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridDetalle.MultiSelect = false;
            gridDetalle.RowHeadersVisible = false;
            gridDetalle.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ModernTheme.StyleDataGrid(gridDetalle);

            if (gridDetalle.Columns.Count == 0)
            {
                gridDetalle.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Producto",
                    DataPropertyName = nameof(InvoiceItemRow.Product),
                    FillWeight = 200
                });
                gridDetalle.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Cantidad",
                    DataPropertyName = nameof(InvoiceItemRow.Quantity),
                    FillWeight = 80
                });
                gridDetalle.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Precio",
                    DataPropertyName = nameof(InvoiceItemRow.Price),
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
                    FillWeight = 100
                });
                gridDetalle.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = "Subtotal",
                    DataPropertyName = nameof(InvoiceItemRow.LineTotal),
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
                    FillWeight = 110
                });
            }

            _bsInvoices.DataSource = _invoices;
            gridFacturas.DataSource = _bsInvoices;
            _bsItems.DataSource = _items;
            gridDetalle.DataSource = _bsItems;
        }

        private void LoadCustomerList()
        {
            _loadingCustomers = true;
            _customers.Clear();
            _customers.Add(new CustomerOption { Id = null, Display = "Todos los clientes", Email = string.Empty });

            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand(@"SELECT Id, Name, Email FROM dbo.Customers ORDER BY Name ASC", cn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string name = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
                    string email = rd.IsDBNull(2) ? string.Empty : rd.GetString(2);
                    _customers.Add(new CustomerOption
                    {
                        Id = rd.GetInt32(0),
                        Display = string.IsNullOrWhiteSpace(name)
                            ? (string.IsNullOrWhiteSpace(email) ? "(Sin nombre)" : email)
                            : name,
                        Email = email
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible cargar los clientes:\n" + ex.Message, "Historial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            cboCliente.DataSource = null;
            cboCliente.DataSource = _customers;
            cboCliente.DisplayMember = nameof(CustomerOption.Display);
            cboCliente.ValueMember = nameof(CustomerOption.Id);
            cboCliente.SelectedIndex = 0;

            _loadingCustomers = false;
        }

        private CustomerContext ResolveContext()
        {
            if (_isAdmin)
            {
                if (cboCliente.SelectedItem is CustomerOption opt)
                    return new CustomerContext(opt.Id, opt.Email);
                return new CustomerContext(null, null);
            }

            return new CustomerContext(AppSession.CustomerId, AppSession.UserEmail);
        }

        private void LoadInvoices()
        {
            lblMensaje.Visible = false;
            _invoices.Clear();

            try
            {
                var ctx = ResolveContext();
                using var cn = Database.OpenConnection();

                string sql = @"
SELECT i.Id, i.Number, i.IssuedAt, i.PaymentMethod, i.PaymentStatus, i.Subtotal, i.Tax, i.Total,
       COALESCE(NULLIF(i.Customer,''), NULLIF(c.Name,'')) AS CustomerName,
       COALESCE(NULLIF(i.CustomerEmail,''), NULLIF(c.Email,'')) AS CustomerEmail
FROM dbo.Invoices i
LEFT JOIN dbo.Customers c ON i.CustomerId = c.Id
WHERE i.IssuedAt >= @from AND i.IssuedAt < @to";

                string search = (txtBuscar.Text ?? string.Empty).Trim();
                bool hasSearch = !string.IsNullOrWhiteSpace(search);
                if (ctx.CustomerId.HasValue)
                {
                    sql += " AND i.CustomerId = @cid";
                }
                else if (!string.IsNullOrEmpty(ctx.Email))
                {
                    sql += " AND (ISNULL(i.CustomerEmail,'') = @cemail OR ISNULL(c.Email,'') = @cemail)";
                }

                string estado = cboEstado.SelectedItem?.ToString() ?? "Todos";
                if (!string.Equals(estado, "Todos", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND ISNULL(i.PaymentStatus,'') = @status";
                }

                string metodo = cboMetodoPago.SelectedItem?.ToString() ?? "Todos";
                if (!string.Equals(metodo, "Todos", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND ISNULL(i.PaymentMethod,'') = @method";
                }

                if (hasSearch)
                {
                    sql += @" AND (
    i.Number LIKE @search OR
    EXISTS (SELECT 1 FROM dbo.InvoiceItems it WHERE it.InvoiceId = i.Id AND (it.Name LIKE @search OR it.Code LIKE @search))
)";
                }

                sql += " ORDER BY i.IssuedAt DESC";

                using var cmd = new SqlCommand(sql, cn);
                var fromDate = dtDesde.Value.Date;
                var toDate = dtHasta.Value.Date;
                if (fromDate > toDate)
                {
                    var temp = fromDate;
                    fromDate = toDate;
                    toDate = temp;
                }

                cmd.Parameters.AddWithValue("@from", fromDate);
                cmd.Parameters.AddWithValue("@to", toDate.AddDays(1));

                if (ctx.CustomerId.HasValue)
                    cmd.Parameters.AddWithValue("@cid", ctx.CustomerId.Value);
                else if (!string.IsNullOrEmpty(ctx.Email))
                    cmd.Parameters.AddWithValue("@cemail", ctx.Email);

                if (!string.Equals(estado, "Todos", StringComparison.OrdinalIgnoreCase))
                    cmd.Parameters.AddWithValue("@status", estado);
                if (!string.Equals(metodo, "Todos", StringComparison.OrdinalIgnoreCase))
                    cmd.Parameters.AddWithValue("@method", metodo);
                if (hasSearch)
                    cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    _invoices.Add(new InvoiceRow
                    {
                        Id = rd.GetInt32(0),
                        Number = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                        IssuedAt = rd.GetDateTime(2),
                        PaymentMethod = rd.IsDBNull(3) ? string.Empty : rd.GetString(3),
                        PaymentStatus = rd.IsDBNull(4) ? string.Empty : rd.GetString(4),
                        Subtotal = rd.GetDecimal(5),
                        Tax = rd.GetDecimal(6),
                        Total = rd.GetDecimal(7),
                        CustomerName = rd.IsDBNull(8) ? string.Empty : rd.GetString(8),
                        CustomerEmail = rd.IsDBNull(9) ? string.Empty : rd.GetString(9)
                    });
                }
            }
            catch (Exception ex)
            {
                lblMensaje.Text = "No se pudo cargar el historial, intenta nuevamente.";
                lblMensaje.Visible = true;
                MessageBox.Show("Error cargando historial:\n" + ex.Message, "Historial", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            gridFacturas.Refresh();
            UpdateTotalsSummary();

            if (_invoices.Count == 0)
            {
                lblMensaje.Text = _isAdmin ? "No hay compras registradas para el criterio seleccionado." : "Aún no tienes compras registradas.";
                lblMensaje.Visible = true;
            }
            else
            {
                lblMensaje.Visible = false;
                gridFacturas.ClearSelection();
                if (gridFacturas.Rows.Count > 0)
                    gridFacturas.Rows[0].Selected = true;
            }

            LoadInvoiceDetails();
        }

        private void UpdateTotalsSummary()
        {
            decimal total = _invoices.Sum(i => i.Total);
            lblTotalGastado.Text = total.ToString("C2");

            if (_isAdmin && cboCliente.SelectedItem is CustomerOption opt)
            {
                lblClienteResumenTitulo.Text = "Cliente";
                lblClienteResumen.Text = string.IsNullOrWhiteSpace(opt.Email)
                    ? opt.Display
                    : string.Format("{0} ({1})", opt.Display, opt.Email);
            }
            else if (!_isAdmin)
            {
                // CAMBIO: Solo mostrar el correo del usuario logueado
                lblClienteResumenTitulo.Text = "Correo";
                lblClienteResumen.Text = AppSession.UserEmail ?? "";
            }
            else
            {
                lblClienteResumenTitulo.Text = "Cliente";
                lblClienteResumen.Text = "";
            }
        }

        private void LoadInvoiceDetails()
        {
            _items.Clear();

            if (gridFacturas.CurrentRow?.DataBoundItem is not InvoiceRow row)
            {
                invoiceInfoPanel.Visible = false;  // CAMBIO: Ocultar panel
                return;
            }

            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand(@"SELECT Name, Qty, Price, Subtotal FROM dbo.InvoiceItems WHERE InvoiceId = @id ORDER BY Id", cn);
                cmd.Parameters.AddWithValue("@id", row.Id);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    _items.Add(new InvoiceItemRow
                    {
                        Product = rd.IsDBNull(0) ? string.Empty : rd.GetString(0),
                        Quantity = rd.GetInt32(1),
                        Price = rd.GetDecimal(2),
                        LineTotal = rd.GetDecimal(3)
                    });
                }

                // CAMBIO: Actualizar los labels y mostrar el panel
                lblDetalleTitulo.Text = $"Factura {row.Number}";
                lblDetalleInfo.Text = string.Format("{0:g} • Total: {1:C2} • {2} • {3}",
                    row.IssuedAt,
                    row.Total,
                    string.IsNullOrWhiteSpace(row.PaymentMethod) ? "Sin método" : row.PaymentMethod,
                    string.IsNullOrWhiteSpace(row.PaymentStatus) ? "Sin estado" : row.PaymentStatus);

                invoiceInfoPanel.Visible = true;  // CAMBIO: Mostrar panel
            }
            catch (Exception ex)
            {
                lblDetalleInfo.Text = "No se pudieron cargar los detalles.";
                invoiceInfoPanel.Visible = true;
                MessageBox.Show("Error cargando el detalle:\n" + ex.Message, "Historial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private sealed class CustomerOption
        {
            public int? Id { get; set; }
            public string Display { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public override string ToString() => Display;
        }

        private readonly record struct CustomerContext(int? CustomerId, string? Email);

        private sealed class InvoiceRow
        {
            public int Id { get; set; }
            public string Number { get; set; } = string.Empty;
            public DateTime IssuedAt { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public string PaymentStatus { get; set; } = string.Empty;
            public decimal Subtotal { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
        }

        private sealed class InvoiceItemRow
        {
            public string Product { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal LineTotal { get; set; }
        }
    }
}