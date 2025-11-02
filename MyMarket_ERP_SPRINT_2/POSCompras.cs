using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class POSCompras : Form
    {
        // IVA (puedes cambiarlo a tu necesidad)
        const decimal TAX_RATE = 0.19m;

        // Estado de venta
        private BindingList<CartItem> _cart = new();
        private List<Product> _cacheProducts = new(); // cache para autocompletar
        private readonly AutoCompleteStringCollection _customerSuggestions = new();
        private bool _notifiedCustomerError = false;

        // Info sesión (opcional: set por login)
        private readonly string _cashierEmail;
        private readonly ElectronicInvoiceProcessor _invoiceProcessor = ElectronicInvoiceProcessor.CreateDefault();
        private static readonly Regex EmailRegex =
            new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);

        public POSCompras(string cashierEmail = "")
        {
            _cashierEmail = cashierEmail;
            InitializeComponent();
            var role = AppSession.Role;         // toma el rol guardado en login
            this.Tag = NavSection.Compras;      // marca sección actual

            SidebarInstaller.Install(
                this,
                role,
                NavSection.Compras,
                section => NavigationService.Open(section, this, role)
            );

            // Inicializa BD / tablas
            InitDb();

            // Preparar UI
            SetupGrid();
            LoadProductsCache();
            SetupAutocomplete();
            ConfigureCustomerAutocomplete();

            cboPago.Items.AddRange(new object[] { "Efectivo", "Tarjeta", "Transferencia" });
            cboPago.SelectedIndex = 0;

            numQty.Minimum = 1; numQty.Maximum = 999;
            chkIVA.Checked = true;

            // Eventos
            txtScan.KeyDown += TxtScan_KeyDown;      // Enter para agregar
            btnAgregar.Click += (_, __) => AddByScanOrName();
            btnBuscar.Click += (_, __) => BuscarProducto();
            btnEliminarSel.Click += (_, __) => RemoveSelected();
            gridCart.CellDoubleClick += (_, e) => EditRowQty(e.RowIndex);
            btnCobrar.Click += (_, __) => Cobrar();
            btnCancelar.Click += (_, __) => CancelarVenta();
            chkIVA.CheckedChanged += (_, __) => RecalcTotals();
            _cart.ListChanged += (_, __) => RecalcTotals();
            Load += POSCompras_Load;
        }

        private async void POSCompras_Load(object? sender, EventArgs e)
        {
            await LoadCustomerSuggestionsAsync();
        }

        // ========== BD ==========
        private void InitDb()
        {
            // El esquema y la semilla inicial se gestionan desde Database.EnsureInitialized().
        }

        private void LoadProductsCache()
        {
            _cacheProducts.Clear();
            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand("SELECT Id, Code, Name, Price, Stock, IsActive FROM dbo.Products WHERE IsActive = 1;", cn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    _cacheProducts.Add(new Product
                    {
                        Id = rd.GetInt32(0),
                        Code = rd.GetString(1),
                        Name = rd.GetString(2),
                        Price = rd.GetDecimal(3),
                        Stock = rd.GetInt32(4),
                        IsActive = rd.GetBoolean(5)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando productos:\n" + ex.Message, "POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== UI ==========
        private void SetupGrid()
        {
            gridCart.AutoGenerateColumns = false;
            gridCart.ReadOnly = true;
            gridCart.RowHeadersVisible = false;
            gridCart.AllowUserToAddRows = false;
            gridCart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridCart.MultiSelect = false;

            gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Código", DataPropertyName = "Code", Width = 90 });
            gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "Name", Width = 260 });
            gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "Price", Width = 90, DefaultCellStyle = { Format = "C2" } });
            gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant.", DataPropertyName = "Qty", Width = 60 });
            gridCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", DataPropertyName = "LineTotal", Width = 110, DefaultCellStyle = { Format = "C2" } });

            gridCart.DataSource = _cart;
        }

        private void SetupAutocomplete()
        {
            var ac = new AutoCompleteStringCollection();
            ac.AddRange(_cacheProducts.Select(p => p.Code).ToArray());
            ac.AddRange(_cacheProducts.Select(p => p.Name).ToArray());
            txtScan.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtScan.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtScan.AutoCompleteCustomSource = ac;
        }

        private void ConfigureCustomerAutocomplete()
        {
            txtCliente.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtCliente.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtCliente.AutoCompleteCustomSource = _customerSuggestions;
        }

        private async Task LoadCustomerSuggestionsAsync()
        {
            try
            {
                var names = await Task.Run(() =>
                {
                    var list = new List<string>();
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand("SELECT Name FROM dbo.Customers ORDER BY Name ASC;", cn);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        if (rd.IsDBNull(0))
                            continue;

                        var name = rd.GetString(0).Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            list.Add(name);
                    }
                    return list;
                });

                var distinct = names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                _customerSuggestions.Clear();
                if (distinct.Length > 0)
                    _customerSuggestions.AddRange(distinct);
            }
            catch (SqlException)
            {
                if (_notifiedCustomerError || IsDisposed)
                    return;

                _notifiedCustomerError = true;
                MessageBox.Show(
                    "No fue posible cargar la lista de clientes. Verifica la conexión a la base de datos.",
                    "Compras",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                if (_notifiedCustomerError || IsDisposed)
                    return;

                _notifiedCustomerError = true;
                MessageBox.Show(
                    "No fue posible cargar las sugerencias de clientes.",
                    "Compras",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // ========== Buscar / Agregar ==========
        private void TxtScan_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { AddByScanOrName(); e.Handled = true; e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.F2) { BuscarProducto(); e.Handled = true; }
        }

        private void AddByScanOrName()
        {
            string q = (txtScan.Text ?? "").Trim();
            int qty = (int)numQty.Value;
            if (qty <= 0) qty = 1;

            if (string.IsNullOrEmpty(q))
            {
                txtScan.Focus(); return;
            }

            // intentar por código exacto
            var prod = _cacheProducts.FirstOrDefault(p => p.Code.Equals(q, StringComparison.OrdinalIgnoreCase));

            // si no, por nombre (si hay una coincidencia única)
            if (prod == null)
            {
                var matches = _cacheProducts.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 1) prod = matches[0];
                else if (matches.Count > 1)
                {
                    // abrir diálogo de selección
                    using var dlg = new BuscarProductoDialog(_cacheProducts, q);
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Selected != null)
                        prod = dlg.Selected;
                }
            }

            if (prod == null)
            {
                MessageBox.Show("Producto no encontrado.", "POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtScan.SelectAll(); txtScan.Focus();
                return;
            }

            if (prod.Stock < qty)
            {
                MessageBox.Show($"Stock insuficiente. Stock actual: {prod.Stock}", "POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Si ya está en carrito, sumar cantidad
            var line = _cart.FirstOrDefault(c => c.ProductId == prod.Id);
            if (line != null)
            {
                if (prod.Stock < line.Qty + qty)
                {
                    MessageBox.Show($"Stock insuficiente para aumentar cantidad. Stock: {prod.Stock}", "POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                line.Qty += qty;
                _cart.ResetItem(_cart.IndexOf(line));
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = prod.Id,
                    Code = prod.Code,
                    Name = prod.Name,
                    Price = prod.Price,
                    Qty = qty
                });
            }

            txtScan.Clear();
            numQty.Value = 1;
            txtScan.Focus();
        }

        private void BuscarProducto()
        {
            using var dlg = new BuscarProductoDialog(_cacheProducts);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Selected != null)
            {
                txtScan.Text = dlg.Selected.Code;
                AddByScanOrName();
            }
        }

        private void RemoveSelected()
        {
            if (gridCart.CurrentRow == null) return;
            if (gridCart.CurrentRow.DataBoundItem is CartItem ci)
                _cart.Remove(ci);
        }

        private void EditRowQty(int rowIndex)
        {
            if (rowIndex < 0) return;
            if (gridCart.Rows[rowIndex].DataBoundItem is CartItem ci)
            {
                using var di = new Form
                {
                    Text = $"Editar cantidad - {ci.Name}",
                    StartPosition = FormStartPosition.CenterParent,
                    ClientSize = new Size(260, 120),
                    FormBorderStyle = FormBorderStyle.FixedDialog
                };
                var n = new NumericUpDown { Left = 20, Top = 20, Width = 100, Minimum = 1, Maximum = 999, Value = ci.Qty };
                var ok = new Button { Text = "OK", Left = 150, Top = 20, Width = 80, DialogResult = DialogResult.OK };
                di.Controls.AddRange(new Control[] { n, ok });

                if (di.ShowDialog(this) == DialogResult.OK)
                {
                    var prod = _cacheProducts.First(p => p.Id == ci.ProductId);
                    if (n.Value > prod.Stock)
                    {
                        MessageBox.Show($"Stock insuficiente. Stock: {prod.Stock}", "POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    ci.Qty = (int)n.Value;
                    _cart.ResetItem(_cart.IndexOf(ci));
                }
            }
        }

        private void RecalcTotals()
        {
            decimal subtotal = _cart.Sum(c => c.LineTotal);
            decimal tax = chkIVA.Checked ? Math.Round(subtotal * TAX_RATE, 2) : 0m;
            decimal total = subtotal + tax;

            lblSubtotal.Text = subtotal.ToString("C2");
            lblIVA.Text = tax.ToString("C2");
            lblTotal.Text = total.ToString("C2");
        }

        private CustomerMatch? ResolveCustomer(SqlConnection cn, string rawInput)
        {
            string trimmed = (rawInput ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return null;

            string? email = ExtractEmail(trimmed);
            if (!string.IsNullOrEmpty(email))
            {
                var found = FindCustomerBy(cn, "Email", email);
                if (found != null)
                    return found;
            }

            var byDocument = FindCustomerBy(cn, "Document", trimmed);
            if (byDocument != null)
                return byDocument;

            return FindCustomerBy(cn, "Name", trimmed);
        }

        private static CustomerMatch? FindCustomerBy(SqlConnection cn, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string sql = column switch
            {
                "Email" => "SELECT TOP (1) Id, Name, Email, Document FROM dbo.Customers WHERE Email = @v",
                "Document" => "SELECT TOP (1) Id, Name, Email, Document FROM dbo.Customers WHERE Document = @v",
                _ => "SELECT TOP (1) Id, Name, Email, Document FROM dbo.Customers WHERE Name = @v"
            };

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@v", value);
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                return new CustomerMatch
                {
                    Id = rd.GetInt32(0),
                    Name = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                    Email = rd.IsDBNull(2) ? string.Empty : rd.GetString(2),
                    Document = rd.IsDBNull(3) ? string.Empty : rd.GetString(3)
                };
            }

            return null;
        }

        private static string? ExtractEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = EmailRegex.Match(value);
            return match.Success ? match.Value : null;
        }

        // ========== Cobro / Facturación ==========
        private void Cobrar()
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("No hay productos en el carrito.", "POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Confirmación rápida de pago
            var pay = cboPago.SelectedItem?.ToString() ?? "Efectivo";
            var cliente = (txtCliente.Text ?? "").Trim();

            // Totales
            decimal subtotal = _cart.Sum(c => c.LineTotal);
            decimal tax = chkIVA.Checked ? Math.Round(subtotal * TAX_RATE, 2) : 0m;
            decimal total = subtotal + tax;

            // Generar número de factura simple
            DateTime issuedAt = DateTime.Now;
            string number = "FAC-" + issuedAt.ToString("yyyyMMdd-HHmmss");

            try
            {
                using var cn = Database.OpenConnection();
                var match = ResolveCustomer(cn, cliente);
                using var tx = cn.BeginTransaction();

                string? customerName = match?.Name;
                if (string.IsNullOrWhiteSpace(customerName))
                    customerName = string.IsNullOrWhiteSpace(cliente) ? null : cliente;

                string? customerEmail = match?.Email;
                if (string.IsNullOrWhiteSpace(customerEmail))
                    customerEmail = ExtractEmail(cliente);
                string? customerDocument = match == null || string.IsNullOrWhiteSpace(match.Document)
                    ? null
                    : match.Document;

                int invoiceId;
                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Invoices(Number,IssuedAt,CashierEmail,Customer,CustomerEmail,CustomerId,PaymentMethod,PaymentStatus,Subtotal,Tax,Total)
                    VALUES(@n,@d,@c,@u,@ue,@uid,@pm,@ps,@st,@tx,@tt);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@n", number);
                    cmd.Parameters.AddWithValue("@d", issuedAt);
                    cmd.Parameters.AddWithValue("@c", string.IsNullOrEmpty(_cashierEmail) ? (object)DBNull.Value : _cashierEmail);
                    cmd.Parameters.AddWithValue("@u", string.IsNullOrEmpty(customerName) ? (object)DBNull.Value : customerName);
                    cmd.Parameters.AddWithValue("@ue", string.IsNullOrEmpty(customerEmail) ? (object)DBNull.Value : customerEmail);
                    cmd.Parameters.AddWithValue("@uid", match is null ? (object)DBNull.Value : match.Id);
                    cmd.Parameters.AddWithValue("@pm", pay);
                    cmd.Parameters.AddWithValue("@ps", "Pagada");
                    cmd.Parameters.AddWithValue("@st", subtotal);
                    cmd.Parameters.AddWithValue("@tx", tax);
                    cmd.Parameters.AddWithValue("@tt", total);
                    invoiceId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var it in _cart)
                {
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO dbo.InvoiceItems(InvoiceId,ProductId,Code,Name,Qty,Price,Subtotal)
                        VALUES(@i,@p,@c,@n,@q,@pr,@s);", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@i", invoiceId);
                        cmd.Parameters.AddWithValue("@p", it.ProductId);
                        cmd.Parameters.AddWithValue("@c", it.Code);
                        cmd.Parameters.AddWithValue("@n", it.Name);
                        cmd.Parameters.AddWithValue("@q", it.Qty);
                        cmd.Parameters.AddWithValue("@pr", it.Price);
                        cmd.Parameters.AddWithValue("@s", it.LineTotal);
                        cmd.ExecuteNonQuery();
                    }

                    using (var stock = new SqlCommand("UPDATE dbo.Products SET Stock = Stock - @q WHERE Id=@id AND Stock >= @q;", cn, tx))
                    {
                        stock.Parameters.AddWithValue("@q", it.Qty);
                        stock.Parameters.AddWithValue("@id", it.ProductId);
                        int affected = stock.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            tx.Rollback();
                            MessageBox.Show($"Stock insuficiente al confirmar: {it.Name}", "POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                tx.Commit();

                ElectronicInvoiceResult? einvoiceResult = null;
                try
                {
                    var items = _cart
                        .Select((it, index) => new ElectronicInvoiceItem
                        {
                            LineNumber = index + 1,
                            Code = it.Code,
                            Description = it.Name,
                            Quantity = it.Qty,
                            UnitPrice = it.Price,
                            LineTotal = it.LineTotal,
                            TaxRate = chkIVA.Checked ? TAX_RATE : 0m
                        })
                        .ToList();

                    var context = new ElectronicInvoiceContext
                    {
                        InvoiceId = invoiceId,
                        Number = number,
                        IssuedAt = issuedAt,
                        CashierEmail = string.IsNullOrEmpty(_cashierEmail) ? null : _cashierEmail,
                        CustomerName = customerName,
                        CustomerEmail = customerEmail,
                        CustomerDocument = customerDocument,
                        PaymentMethod = pay,
                        PaymentStatus = "Pagada",
                        Subtotal = subtotal,
                        Tax = tax,
                        Total = total,
                        Currency = "COP",
                        Items = items
                    };

                    einvoiceResult = _invoiceProcessor.Process(context);
                }
                catch (Exception einvoiceEx)
                {
                    einvoiceResult = ElectronicInvoiceResult.FailureResult("Error al preparar la factura electrónica: " + einvoiceEx.Message, null, null);
                }

                if (einvoiceResult != null)
                {
                    try
                    {
                        using var update = new SqlCommand(@"UPDATE dbo.Invoices SET ElectronicInvoiceXml=@xml, RegulatorTrackingId=@track, RegulatorStatus=@status, RegulatorResponseMessage=@msg WHERE Id=@id;", cn);
                        update.Parameters.AddWithValue("@xml", string.IsNullOrEmpty(einvoiceResult.SignedXml) ? (object)DBNull.Value : einvoiceResult.SignedXml);
                        update.Parameters.AddWithValue("@track", string.IsNullOrWhiteSpace(einvoiceResult.TrackingId) ? (object)DBNull.Value : einvoiceResult.TrackingId);
                        update.Parameters.AddWithValue("@status", einvoiceResult.Success ? "Aceptada" : "Error");
                        update.Parameters.AddWithValue("@msg", string.IsNullOrWhiteSpace(einvoiceResult.Message) ? (object)DBNull.Value : einvoiceResult.Message);
                        update.Parameters.AddWithValue("@id", invoiceId);
                        update.ExecuteNonQuery();
                    }
                    catch (Exception persistEx)
                    {
                        MessageBox.Show("Factura generada pero no se pudo registrar el estado electrónico:\n" + persistEx.Message, "POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                LoadProductsCache();
                SetupAutocomplete();

                string message = $"Venta realizada.\nFactura: {number}\nTotal: {total:C2}";
                if (einvoiceResult != null)
                {
                    string statusText = einvoiceResult.Success ? "Factura electrónica enviada." : "Factura electrónica pendiente.";
                    message += "\n\n" + statusText;

                    if (!string.IsNullOrWhiteSpace(einvoiceResult.Message))
                        message += "\n" + einvoiceResult.Message;

                    if (!string.IsNullOrWhiteSpace(einvoiceResult.TrackingId))
                        message += "\nTracking: " + einvoiceResult.TrackingId;
                }

                MessageBox.Show(message, "POS", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _cart.Clear();
                txtCliente.Clear();
                RecalcTotals();
                txtScan.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cobrar:\n" + ex.Message, "POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelarVenta()
        {
            if (_cart.Count == 0) { txtScan.Focus(); return; }
            if (MessageBox.Show("¿Cancelar la venta actual?", "POS", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _cart.Clear();
                txtCliente.Clear();
                RecalcTotals();
            }
        }
    }

    // ======= Modelos =======
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }

    public class CartItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        private int _qty = 1;
        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(nameof(Qty)); OnPropertyChanged(nameof(LineTotal)); }
        }
        public decimal LineTotal => Price * Qty;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ======= Diálogo búsqueda =======
    internal class BuscarProductoDialog : Form
    {
        private DataGridView grid = new();
        private TextBox txt = new();
        private Button btnOk = new() { Text = "Seleccionar", DialogResult = DialogResult.OK };
        private readonly List<Product> _products;
        public Product? Selected { get; private set; }

        public BuscarProductoDialog(List<Product> products, string query = "")
        {
            _products = products;
            Text = "Buscar producto";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;

            txt.Dock = DockStyle.Top;
            txt.Margin = new Padding(12);
            txt.Text = query;
            txt.TextChanged += (_, __) => ApplyFilter();

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToAddRows = false;
            grid.AutoGenerateColumns = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Código", DataPropertyName = "Code", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "Name", Width = 280 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "Price", Width = 100, DefaultCellStyle = { Format = "C2" } });
            grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) SelectRow(e.RowIndex); };

            btnOk.Dock = DockStyle.Bottom;
            btnOk.Height = 42;
            btnOk.Click += (_, __) => { if (grid.CurrentRow != null) SelectRow(grid.CurrentRow.Index); };

            Controls.Add(grid);
            Controls.Add(btnOk);
            Controls.Add(txt);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string q = (txt.Text ?? "").Trim().ToLowerInvariant();
            var data = string.IsNullOrEmpty(q)
                ? _products
                : _products.Where(p => p.Code.ToLowerInvariant().Contains(q) || p.Name.ToLowerInvariant().Contains(q)).ToList();
            grid.DataSource = new BindingList<Product>(data);
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= grid.Rows.Count) return;
            if (grid.Rows[index].DataBoundItem is Product p)
            {
                Selected = p;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }

    internal sealed class CustomerMatch
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Document { get; set; }
    }
}
