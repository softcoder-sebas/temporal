using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class Inventario : Form
    {
        private readonly BindingList<PurchaseOrder> _allOrders = new();
        private readonly BindingSource _bsOrders = new();
        private List<PurchaseOrder> _filteredOrders = new();
        private int _page = 1;
        private const int PageSize = 10;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_filteredOrders.Count / PageSize));
        private SplitContainer _split;
        private bool _detalleVisible = false;
        private const int DetailWidth = 360;
        private Panel _contentHost;

        private readonly BindingList<ProductoInventario> _allProducts = new();
        private readonly BindingSource _bsProducts = new();

        public Inventario()
        {
            InitializeComponent();
            var role = AppSession.Role;
            this.Tag = NavSection.Inventario;

            _contentHost = SidebarInstaller.Install(
                this,
                role,
                NavSection.Inventario,
                section => NavigationService.Open(section, this, role)
            );

            if (layoutRoot.Parent == this) Controls.Remove(layoutRoot);
            _contentHost.Controls.Add(layoutRoot);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.BringToFront();
            _contentHost.ResumeLayout(true);
            _contentHost.PerformLayout();

            SetupGridOrdenes();
            SetupGridProductos();

            cmbEstado.Items.AddRange(new object[] { "Todos los estados", "Pendiente", "Aprobado", "Recibido", "Cotizado" });
            cmbEstado.SelectedIndex = 0;

            cmbCategoriaProducto.Items.AddRange(new object[] { "Todas las categorías", "Abarrotes", "Lácteos", "Carnes", "Verduras", "Bebidas", "Limpieza" });
            cmbCategoriaProducto.SelectedIndex = 0;

            txtBuscar.TextChanged += (_, __) => ApplyFiltersOrdenes();
            cmbEstado.SelectedIndexChanged += (_, __) => ApplyFiltersOrdenes();
            dtDesde.ValueChanged += (_, __) => ApplyFiltersOrdenes();
            dtHasta.ValueChanged += (_, __) => ApplyFiltersOrdenes();
            chkFecha.CheckedChanged += (_, __) => ApplyFiltersOrdenes();
            btnAnterior.Click += (_, __) => { if (_page > 1) { _page--; RefreshPageOrdenes(); } };
            btnSiguiente.Click += (_, __) => { if (_page < TotalPages) { _page++; RefreshPageOrdenes(); } };
            btnNuevaCompra.Click += (_, __) => NuevaCompra();
            grid.CellClick += Grid_CellClick;
            btnRegistrarProveedor.Click += (_, __) => RegistrarProveedor();
            btnToggleDetalle.Click += (_, __) => ToggleDetalle();
            UpdateDetalleToggleText();

            txtBuscarProducto.TextChanged += (_, __) => ApplyFiltersProductos();
            cmbCategoriaProducto.SelectedIndexChanged += (_, __) => ApplyFiltersProductos();
            chkSoloCritico.CheckedChanged += (_, __) => ApplyFiltersProductos();
            btnNuevoProducto.Click += (_, __) => NuevoProducto();
            btnEditarProducto.Click += (_, __) => EditarProducto();
            btnEliminarProducto.Click += (_, __) => EliminarProducto();
            btnRefrescarProductos.Click += (_, __) => LoadProductos();
            gridProductos.CellFormatting += GridProductos_CellFormatting;

            SeedDemoOrdenes();
            ApplyFiltersOrdenes();
            LoadProductos();

            this.Shown += Inventario_Shown;
        }

        private void Inventario_Shown(object sender, EventArgs e)
        {
            if (_split == null && grid.Parent is Panel parent)
            {
                BuildSplitOrdenes(parent);
            }
        }

        private void BuildSplitOrdenes(Panel host)
        {
            _split = new SplitContainer
            {
                Orientation = Orientation.Vertical,
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel2,
                Panel2MinSize = 0,
                Panel1MinSize = 100,
                SplitterWidth = 6
            };

            host.Controls.Add(_split);
            _split.BringToFront();

            grid.Parent = _split.Panel1;
            grid.Dock = DockStyle.Fill;

            pnlDetalle.Parent = _split.Panel2;
            pnlDetalle.Dock = DockStyle.Fill;

            _split.SizeChanged += (_, __) => AdjustSplitter();

            AdjustSplitter();
        }

        private void AdjustSplitter()
        {
            if (_split == null) return;

            int width = _split.ClientSize.Width;
            int min = _split.Panel1MinSize;
            int max = Math.Max(min, width - _split.Panel2MinSize - _split.SplitterWidth);

            if (width <= (min + _split.Panel2MinSize + _split.SplitterWidth))
            {
                _split.Panel2Collapsed = !_detalleVisible;
                return;
            }

            if (!_detalleVisible)
            {
                _split.Panel2Collapsed = true;
            }
            else
            {
                _split.Panel2Collapsed = false;
                int desired = Math.Max(100, width - DetailWidth - _split.SplitterWidth);
                int safe = Math.Min(Math.Max(desired, min), max);
                if (_split.SplitterDistance != safe)
                    _split.SplitterDistance = safe;
            }
        }

        private void ToggleDetalle()
        {
            _detalleVisible = !_detalleVisible;
            AdjustSplitter();
            UpdateDetalleToggleText();
        }

        private void UpdateDetalleToggleText()
        {
            if (btnToggleDetalle == null) return;
            btnToggleDetalle.Text = _detalleVisible ? "◀ Ocultar detalle" : "▶ Mostrar detalle";
        }

        private void SetupGridOrdenes()
        {
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            ModernTheme.StyleDataGrid(grid);

            if (grid.Columns.Count == 0)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "N° Cotización", DataPropertyName = "Code", Width = 100 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Proveedor", DataPropertyName = "Supplier", Width = 220 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha", DataPropertyName = "Date", Width = 110, DefaultCellStyle = { Format = "dd/MM/yyyy" } });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "Total", Width = 120, DefaultCellStyle = { Format = "C2" } });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Status", Width = 110 });
            }
            _bsOrders.DataSource = _filteredOrders;
            grid.DataSource = _bsOrders;
        }

        private void SeedDemoOrdenes()
        {
            string[] suppliers = { "Distribuidora ABC", "Proveedor XYZ", "Suministros DEF" };
            string[] status = { "Pendiente", "Aprobado", "Recibido", "Cotizado" };
            var rnd = new Random(2);

            for (int i = 1; i <= 25; i++)
            {
                var po = new PurchaseOrder
                {
                    Code = string.Format("{0}-{1:000}", i % 3 == 0 ? "SU" : i % 2 == 0 ? "PO" : "DI", i),
                    Supplier = suppliers[rnd.Next(suppliers.Length)],
                    Date = new DateTime(2025, 1, 1).AddDays(i + rnd.Next(0, 3)),
                    Status = status[rnd.Next(status.Length)]
                };
                int items = rnd.Next(1, 4);
                for (int k = 1; k <= items; k++)
                {
                    po.Items.Add(new PurchaseItem
                    {
                        Name = string.Format("Producto {0}", (char)('A' + rnd.Next(0, 5))),
                        Qty = rnd.Next(1, 12),
                        UnitPrice = rnd.Next(20, 500)
                    });
                }
                _allOrders.Add(po);
            }
        }

        private void ApplyFiltersOrdenes()
        {
            IEnumerable<PurchaseOrder> q = _allOrders;

            string text = (txtBuscar.Text ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(text))
                q = q.Where(p => (p.Code?.ToLowerInvariant().Contains(text) ?? false) ||
                                 (p.Supplier?.ToLowerInvariant().Contains(text) ?? false));

            string st = cmbEstado.SelectedItem?.ToString() ?? "Todos los estados";
            if (st != "Todos los estados")
                q = q.Where(p => p.Status.Equals(st, StringComparison.OrdinalIgnoreCase));

            if (chkFecha.Checked)
            {
                var d1 = dtDesde.Value.Date;
                var d2 = dtHasta.Value.Date;
                if (d1 <= d2)
                    q = q.Where(p => p.Date.Date >= d1 && p.Date.Date <= d2);
            }

            _filteredOrders = q.OrderByDescending(p => p.Date).ToList();
            _page = 1;
            RefreshPageOrdenes();
        }

        private void RefreshPageOrdenes()
        {
            int total = _filteredOrders.Count;
            int skip = (_page - 1) * PageSize;
            var page = _filteredOrders.Skip(skip).Take(PageSize).ToList();

            _bsOrders.DataSource = new BindingList<PurchaseOrder>(page);

            lblRango.Text = total == 0 ? "Mostrando 0 a 0 de 0 resultados"
                                       : string.Format("Mostrando {0} a {1} de {2} resultados", skip + 1, skip + page.Count, total);

            btnAnterior.Enabled = _page > 1;
            btnSiguiente.Enabled = _page < TotalPages;

            if (page.Count > 0)
                ShowDetail(page[0]);
            else
                ClearDetail();
        }

        private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_bsOrders[e.RowIndex] is PurchaseOrder po)
            {
                ShowDetail(po);
            }
        }

        private void ShowDetail(PurchaseOrder po)
        {
            lblOrd.Text = po.Code;
            lblProv.Text = po.Supplier;
            lblFecha.Text = po.Date.ToString("dd/MM/yyyy");
            lblEstado.Text = po.Status;

            listProductos.Items.Clear();
            foreach (var it in po.Items)
            {
                var li = new ListViewItem(new[]
                {
                    it.Name,
                    it.UnitPrice.ToString("C2"),
                    string.Format("x{0}", it.Qty),
                    (it.Qty * it.UnitPrice).ToString("C2")
                });
                listProductos.Items.Add(li);
            }
            lblTotal.Text = po.Total.ToString("C2");
        }

        private void ClearDetail()
        {
            lblOrd.Text = "-";
            lblProv.Text = "-";
            lblFecha.Text = "-";
            lblEstado.Text = "-";
            listProductos.Items.Clear();
            lblTotal.Text = "$0.00";
        }

        private void NuevaCompra()
        {
            using var dlg = new NuevaCompraDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                _allOrders.Add(dlg.Result);
                ApplyFiltersOrdenes();
            }
        }

        private void RegistrarProveedor()
        {
            using var dlg = new RegistrarProveedorDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string mensaje = string.Format("Proveedor '{0}' registrado.", dlg.NombreProveedor);
                MessageBox.Show(mensaje, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void SetupGridProductos()
        {
            gridProductos.AutoGenerateColumns = false;
            gridProductos.ReadOnly = true;
            gridProductos.RowHeadersVisible = false;
            gridProductos.AllowUserToAddRows = false;
            gridProductos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridProductos.MultiSelect = false;
            ModernTheme.StyleDataGrid(gridProductos);

            if (gridProductos.Columns.Count == 0)
            {
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Código", DataPropertyName = "Code", Width = 100 });
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Name", Width = 280 });
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Categoría", DataPropertyName = "Category", Width = 140 });
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "Price", Width = 110, DefaultCellStyle = { Format = "C2" } });
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock", DataPropertyName = "Stock", Width = 80 });
                gridProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "StatusDisplay", Width = 120 });
            }

            _bsProducts.DataSource = _allProducts;
            gridProductos.DataSource = _bsProducts;
        }

        private void LoadProductos()
        {
            _allProducts.Clear();
            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand(@"
                    SELECT Id, Code, Name, Price, Stock, IsActive
                    FROM dbo.Products
                    WHERE IsActive = 1
                    ORDER BY Name ASC;", cn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var stock = rd.GetInt32(4);
                    var isActive = rd.GetBoolean(5);

                    _allProducts.Add(new ProductoInventario
                    {
                        Id = rd.GetInt32(0),
                        Code = rd.GetString(1),
                        Name = rd.GetString(2),
                        Category = "Abarrotes",
                        Price = rd.GetDecimal(3),
                        Stock = stock,
                        IsActive = isActive,
                        StatusDisplay = !isActive ? "Inactivo" :
                                       stock <= 0 ? "Agotado" :
                                       stock <= 5 ? "Stock Crítico" : "Disponible"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando productos:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ApplyFiltersProductos();
        }

        private void ApplyFiltersProductos()
        {
            IEnumerable<ProductoInventario> data = _allProducts.Where(p => p.IsActive);

            string q = (txtBuscarProducto.Text ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(q))
            {
                data = data.Where(p =>
                    (p.Code?.ToLowerInvariant().Contains(q) ?? false) ||
                    (p.Name?.ToLowerInvariant().Contains(q) ?? false));
            }

            string cat = cmbCategoriaProducto.SelectedItem?.ToString() ?? "Todas las categorías";
            if (cat != "Todas las categorías")
                data = data.Where(p => p.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));

            if (chkSoloCritico.Checked)
                data = data.Where(p => p.Stock <= 5 && p.IsActive);

            var list = data.ToList();
            _bsProducts.DataSource = new BindingList<ProductoInventario>(list);
            gridProductos.DataSource = _bsProducts;

            int criticos = _allProducts.Count(p => p.Stock <= 5 && p.IsActive);
            lblConteoProductos.Text = string.Format("Productos: {0} | Stock crítico: {1}", list.Count, criticos);
        }

        private void GridProductos_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridProductos.Columns[e.ColumnIndex].DataPropertyName == "Stock")
            {
                if (e.Value is int stock)
                {
                    if (stock <= 0)
                    {
                        e.CellStyle.BackColor = System.Drawing.Color.FromArgb(254, 226, 226);
                        e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
                    }
                    else if (stock <= 5)
                    {
                        e.CellStyle.BackColor = System.Drawing.Color.FromArgb(254, 243, 199);
                        e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(180, 83, 9);
                    }
                }
            }

            if (gridProductos.Columns[e.ColumnIndex].DataPropertyName == "StatusDisplay")
            {
                if (e.Value is string status)
                {
                    if (status == "Agotado")
                    {
                        e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
                        e.CellStyle.Font = new System.Drawing.Font(gridProductos.Font, System.Drawing.FontStyle.Bold);
                    }
                    else if (status == "Stock Crítico")
                    {
                        e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(180, 83, 9);
                        e.CellStyle.Font = new System.Drawing.Font(gridProductos.Font, System.Drawing.FontStyle.Bold);
                    }
                }
            }
        }

        private void NuevoProducto()
        {
            using var dlg = new ProductoDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                try
                {
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand(@"
                        INSERT INTO dbo.Products(Code, Name, Price, Stock, IsActive)
                        VALUES(@c, @n, @p, @s, @a);", cn);
                    cmd.Parameters.AddWithValue("@c", dlg.Result.Code);
                    cmd.Parameters.AddWithValue("@n", dlg.Result.Name);
                    cmd.Parameters.AddWithValue("@p", dlg.Result.Price);
                    cmd.Parameters.AddWithValue("@s", dlg.Result.Stock);
                    cmd.Parameters.AddWithValue("@a", dlg.Result.IsActive);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Producto creado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadProductos();
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    MessageBox.Show("Ya existe un producto con ese código.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error guardando producto:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EditarProducto()
        {
            if (gridProductos.CurrentRow == null || gridProductos.CurrentRow.DataBoundItem is not ProductoInventario prod)
            {
                MessageBox.Show("Selecciona un producto para editar.", "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new ProductoDialog(prod);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                try
                {
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand(@"
                        UPDATE dbo.Products
                           SET Code=@c, Name=@n, Price=@p, Stock=@s, IsActive=@a
                         WHERE Id=@id;", cn);
                    cmd.Parameters.AddWithValue("@id", prod.Id);
                    cmd.Parameters.AddWithValue("@c", dlg.Result.Code);
                    cmd.Parameters.AddWithValue("@n", dlg.Result.Name);
                    cmd.Parameters.AddWithValue("@p", dlg.Result.Price);
                    cmd.Parameters.AddWithValue("@s", dlg.Result.Stock);
                    cmd.Parameters.AddWithValue("@a", dlg.Result.IsActive);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Producto actualizado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadProductos();
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    MessageBox.Show("Ya existe otro producto con ese código.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error actualizando producto:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EliminarProducto()
        {
            if (gridProductos.CurrentRow == null || gridProductos.CurrentRow.DataBoundItem is not ProductoInventario prod)
            {
                MessageBox.Show("Selecciona un producto para eliminar.", "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string mensaje = string.Format(
                "¿Deseas desactivar el producto '{0}'?\n\nSe mantendrá oculto en el inventario para evitar problemas con facturación en curso.",
                prod.Name);
            var result = MessageBox.Show(mensaje, "Confirmar desactivación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                using var cn = Database.OpenConnection();
                int references = 0;
                using (var check = new SqlCommand("SELECT COUNT(1) FROM dbo.InvoiceItems WHERE ProductId=@id;", cn))
                {
                    check.Parameters.AddWithValue("@id", prod.Id);
                    var resultCount = check.ExecuteScalar();
                    if (resultCount != null && resultCount != DBNull.Value)
                        references = Convert.ToInt32(resultCount);
                }

                using (var cmd = new SqlCommand("UPDATE dbo.Products SET IsActive = 0 WHERE Id=@id;", cn))
                {
                    cmd.Parameters.AddWithValue("@id", prod.Id);
                    cmd.ExecuteNonQuery();
                }

                string finalMsg = references > 0
                    ? "El producto tiene movimientos registrados, se marcó como inactivo para mantener la integridad de las ventas."
                    : "Producto desactivado correctamente.";

                MessageBox.Show(finalMsg, "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al desactivar el producto:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class PurchaseOrder
    {
        public string Code { get; set; } = "";
        public string Supplier { get; set; } = "";
        public DateTime Date { get; set; }
        public string Status { get; set; } = "Pendiente";
        public BindingList<PurchaseItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Qty * i.UnitPrice);
    }

    public class PurchaseItem
    {
        public string Name { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ProductoInventario
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public string StatusDisplay { get; set; } = "";
    }

    internal class ProductoDialog : Form
    {
        public ProductoInventario Result { get; private set; }

        private TextBox txtCode;
        private TextBox txtName;
        private NumericUpDown numPrice;
        private NumericUpDown numStock;
        private CheckBox chkActive;
        private Button btnGuardar;
        private Button btnCancelar;

        public ProductoDialog(ProductoInventario existing = null)
        {
            Text = existing == null ? "Nuevo Producto" : "Editar Producto";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(460, 280);

            BuildForm();

            if (existing != null)
            {
                txtCode.Text = existing.Code;
                txtName.Text = existing.Name;
                numPrice.Value = existing.Price;
                numStock.Value = existing.Stock;
                chkActive.Checked = existing.IsActive;
                Result = existing;
            }
        }

        private void BuildForm()
        {
            int y = 20;

            AddLabel("Código *", 20, y);
            txtCode = AddTextBox(130, y, 280);
            y += 40;

            AddLabel("Nombre *", 20, y);
            txtName = AddTextBox(130, y, 280);
            y += 40;

            AddLabel("Precio *", 20, y);
            numPrice = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, y),
                Width = 160,
                DecimalPlaces = 2,
                Maximum = 9999999,
                ThousandsSeparator = true
            };
            Controls.Add(numPrice);
            y += 40;

            AddLabel("Stock *", 20, y);
            numStock = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, y),
                Width = 160,
                Maximum = 999999
            };
            Controls.Add(numStock);
            y += 40;

            chkActive = new CheckBox
            {
                Text = "Producto activo",
                Location = new System.Drawing.Point(130, y),
                Checked = true,
                AutoSize = true
            };
            Controls.Add(chkActive);
            y += 40;

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new System.Drawing.Point(230, y),
                Width = 100,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancelar);

            btnGuardar = new Button
            {
                Text = "Guardar",
                Location = new System.Drawing.Point(340, y),
                Width = 100,
                DialogResult = DialogResult.OK
            };
            btnGuardar.Click += BtnGuardar_Click;
            Controls.Add(btnGuardar);

            CancelButton = btnCancelar;
            AcceptButton = btnGuardar;
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y + 3),
                Width = 100,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new System.Drawing.Point(x, y),
                Width = width
            };
            Controls.Add(txt);
            return txt;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("El código es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (Result == null)
                Result = new ProductoInventario();

            Result.Code = txtCode.Text.Trim();
            Result.Name = txtName.Text.Trim();
            Result.Price = numPrice.Value;
            Result.Stock = (int)numStock.Value;
            Result.IsActive = chkActive.Checked;
        }
    }

    internal class NuevaCompraDialog : Form
    {
        private TextBox txtProveedor;
        private DateTimePicker dtFecha;
        private ComboBox cmbEstado;
        private Button btnAgregarItem, btnOk, btnCancel;
        private ListView list;
        public PurchaseOrder? Result { get; private set; }

        public NuevaCompraDialog()
        {
            Text = "Nueva cotización";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(520, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var lblProv = new Label { Text = "Proveedor:", Left = 16, Top = 18, AutoSize = true };
            txtProveedor = new TextBox { Left = 100, Top = 14, Width = 240 };
            var lblF = new Label { Text = "Fecha:", Left = 360, Top = 18, AutoSize = true };
            dtFecha = new DateTimePicker { Left = 410, Top = 14, Width = 100, Format = DateTimePickerFormat.Short };
            var lblE = new Label { Text = "Estado:", Left = 16, Top = 50, AutoSize = true };
            cmbEstado = new ComboBox { Left = 100, Top = 46, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEstado.Items.AddRange(new object[] { "Cotizado", "Pendiente", "Aprobado", "Recibido" });
            cmbEstado.SelectedIndex = 0;

            list = new ListView { Left = 16, Top = 84, Width = 494, Height = 260, View = View.Details, FullRowSelect = true, GridLines = true };
            list.Columns.Add("Producto", 220);
            list.Columns.Add("Precio", 80);
            list.Columns.Add("Cant.", 60);
            list.Columns.Add("Subtotal", 100);

            btnAgregarItem = new Button { Text = "Agregar producto", Left = 16, Top = 352, Width = 150 };
            btnAgregarItem.Click += (_, __) => AddItem();

            btnOk = new Button { Text = "Registrar cotización", Left = 300, Top = 352, Width = 124, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancelar", Left = 440, Top = 352, Width = 80, DialogResult = DialogResult.Cancel };
            AcceptButton = btnOk; CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblProv, txtProveedor, lblF, dtFecha, lblE, cmbEstado, list, btnAgregarItem, btnOk, btnCancel });
        }

        private void AddItem()
        {
            using var di = new Form
            {
                Text = "Producto",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new System.Drawing.Size(320, 160),
                FormBorderStyle = FormBorderStyle.FixedDialog
            };
            var lblN = new Label { Text = "Nombre:", Left = 16, Top = 20, AutoSize = true };
            var txtN = new TextBox { Left = 90, Top = 16, Width = 200 };
            var lblP = new Label { Text = "Precio:", Left = 16, Top = 52, AutoSize = true };
            var numP = new NumericUpDown { Left = 90, Top = 48, Width = 100, DecimalPlaces = 2, Minimum = 0, Maximum = 1000000 };
            var lblQ = new Label { Text = "Cant.:", Left = 16, Top = 84, AutoSize = true };
            var numQ = new NumericUpDown { Left = 90, Top = 80, Width = 100, Minimum = 1, Maximum = 999 };
            var ok = new Button { Text = "OK", Left = 210, Top = 110, Width = 80, DialogResult = DialogResult.OK };
            di.Controls.AddRange(new Control[] { lblN, txtN, lblP, numP, lblQ, numQ, ok });
            di.AcceptButton = ok;

            if (di.ShowDialog(this) == DialogResult.OK)
            {
                decimal subtotal = numP.Value * numQ.Value;
                var li = new ListViewItem(new[] { txtN.Text, numP.Value.ToString("C2"), numQ.Value.ToString(), subtotal.ToString("C2") });
                list.Items.Add(li);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtProveedor.Text))
                {
                    MessageBox.Show("Indica el proveedor.");
                    e.Cancel = true; return;
                }
                if (list.Items.Count == 0)
                {
                    MessageBox.Show("Agrega al menos un producto.");
                    e.Cancel = true; return;
                }

                var po = new PurchaseOrder
                {
                    Code = string.Format("PO-{0:HHmmss}", DateTime.Now),
                    Supplier = txtProveedor.Text.Trim(),
                    Date = dtFecha.Value.Date,
                    Status = cmbEstado.SelectedItem?.ToString() ?? "Cotizado"
                };
                foreach (ListViewItem li in list.Items)
                {
                    po.Items.Add(new PurchaseItem
                    {
                        Name = li.SubItems[0].Text,
                        UnitPrice = decimal.Parse(li.SubItems[1].Text, System.Globalization.NumberStyles.Currency),
                        Qty = int.Parse(li.SubItems[2].Text)
                    });
                }
                Result = po;
            }
        }
    }

    internal class RegistrarProveedorDialog : Form
    {
        private TextBox txtNombre, txtRuc, txtEmail, txtTelefono, txtDireccion;
        private Button btnOk, btnCancel;
        public string NombreProveedor => txtNombre.Text.Trim();

        public RegistrarProveedorDialog()
        {
            Text = "Registrar proveedor";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(460, 290);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            var lblN = new Label { Text = "Nombre*", Left = 16, Top = 18, AutoSize = true };
            txtNombre = new TextBox { Left = 120, Top = 14, Width = 300 };

            var lblR = new Label { Text = "RUC / CUIT", Left = 16, Top = 52, AutoSize = true };
            txtRuc = new TextBox { Left = 120, Top = 48, Width = 180 };

            var lblE = new Label { Text = "Email", Left = 16, Top = 86, AutoSize = true };
            txtEmail = new TextBox { Left = 120, Top = 82, Width = 300 };

            var lblT = new Label { Text = "Teléfono", Left = 16, Top = 120, AutoSize = true };
            txtTelefono = new TextBox { Left = 120, Top = 116, Width = 180 };

            var lblD = new Label { Text = "Dirección", Left = 16, Top = 154, AutoSize = true };
            txtDireccion = new TextBox { Left = 120, Top = 150, Width = 300 };

            btnOk = new Button { Text = "Guardar", Left = 248, Top = 210, Width = 84, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancelar", Left = 336, Top = 210, Width = 84, DialogResult = DialogResult.Cancel };
            AcceptButton = btnOk; CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            { lblN, txtNombre, lblR, txtRuc, lblE, txtEmail, lblT, txtTelefono, lblD, txtDireccion, btnOk, btnCancel });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre es obligatorio.", "Completa los datos",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }
    }
}
