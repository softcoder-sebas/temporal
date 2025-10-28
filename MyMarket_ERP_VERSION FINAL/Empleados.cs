using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsSortOrder = System.Windows.Forms.SortOrder;

namespace MyMarket_ERP
{
    public partial class Empleados : Form
    {
        private readonly BindingList<Employee> _rows = new();
        private readonly BindingSource _bs = new();
        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource? _reloadCts;
        private bool _isLoading;
        private int? _lastKnownSelectionId;

        // detalle desplegable
        private bool _detalleVisible = true;
        public Empleados()
        {
            InitializeComponent();
            var role = AppSession.Role;
            this.Tag = NavSection.Empleados;

            SidebarInstaller.Install(
                this,
                role,
                NavSection.Empleados,
                section => NavigationService.Open(section, this, role)
            );
            GridSetup();

            cmbDept.DataSource = new BindingList<string>(new List<string> { "Todos los departamentos" });
            cmbEstado.DataSource = new BindingList<string>(new List<string> { "Todos los estados", "Activo", "Vacaciones", "Inactivo" });
            cmbEstado.SelectedIndex = 0;

            // eventos
            txtBuscar.TextChanged += (_, __) => ApplyFilters();
            cmbDept.SelectedIndexChanged += (_, __) => ApplyFilters();
            cmbEstado.SelectedIndexChanged += (_, __) => ApplyFilters();
            btnNuevo.Click += (_, __) => NewEmployee();
            btnEditar.Click += (_, __) => EditSelected();
            btnEliminar.Click += (_, __) => DeleteSelected();
            grid.SelectionChanged += (_, __) =>
            {
                PaintDetailFromSelection();
                RememberSelection();
                UpdateActionStates();
            };
            btnToggleDetalle.Click += (_, __) => ToggleDetalle();
            this.Resize += (_, __) => AdjustGridWidth();
            this.FormClosed += (_, __) => DisposeSubscriptions();

            _subscriptions.Add(DataEvents.SubscribeEmpleados(this, payload => _ = RefreshEmployeesAsync(payload.EntityId)));

            Shown += async (_, __) => await RefreshEmployeesAsync();

            // BD
            InitDb();
            UpdateActionStates();
        }

        // ====== BD ======
        private void InitDb()
        {
            // El esquema y la semilla inicial se gestionan desde Database.EnsureInitialized().
        }

        private async Task RefreshEmployeesAsync(int? focusId = null)
        {
            var deptSelected = cmbDept.SelectedItem?.ToString();
            var estadoSelected = cmbEstado.SelectedItem?.ToString();
            var selectedId = focusId ?? _lastKnownSelectionId ?? GetSelectedEmployeeId();
            var sortedColumn = grid.SortedColumn?.DataPropertyName;
            var sortOrder = grid.SortOrder;

            SetLoadingState(true);

            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            var cts = new CancellationTokenSource();
            _reloadCts = cts;

            List<Employee> data;
            try
            {
                data = await Task.Run(() => FetchEmployees(cts.Token), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando empleados:\n" + ex.Message, "Empleados", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            foreach (var employee in data)
            {
                _rows.Add(employee);
            }

            UpdateDepartmentFilter(data, deptSelected);
            RestoreEstadoSelection(estadoSelected);

            ApplyFilters(selectedId);
            RestoreSort(sortedColumn, sortOrder);
            PaintDetailFromSelection();
            SetLoadingState(false);
        }

        private static List<Employee> FetchEmployees(CancellationToken token)
        {
            var result = new List<Employee>();
            using var cn = Database.OpenConnection();
            using var cmd = new SqlCommand(@"SELECT Id,Name,Email,Phone,Department,Position,Status,Salary,HireDate
                                FROM dbo.Employees ORDER BY Name ASC;", cn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                token.ThrowIfCancellationRequested();
                result.Add(new Employee
                {
                    Id = rd.GetInt32(0),
                    Name = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    Email = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    Phone = rd.IsDBNull(3) ? "" : rd.GetString(3),
                    Department = rd.IsDBNull(4) ? "" : rd.GetString(4),
                    Position = rd.IsDBNull(5) ? "" : rd.GetString(5),
                    Status = rd.IsDBNull(6) ? "Activo" : rd.GetString(6),
                    Salary = rd.IsDBNull(7) ? 0 : rd.GetDecimal(7),
                    HireDate = rd.IsDBNull(8) ? (DateTime?)null : rd.GetDateTime(8)
                });
            }

            return result;
        }

        // ====== UI ======
        private void GridSetup()
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
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Name", Width = 220 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Departamento", DataPropertyName = "Department", Width = 140 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cargo", DataPropertyName = "Position", Width = 160 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Status", Width = 100 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Salario", DataPropertyName = "Salary", Width = 120, DefaultCellStyle = { Format = "C0" } });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ingreso", DataPropertyName = "HireDate", Width = 100, DefaultCellStyle = { Format = "dd/MM/yyyy" } });
            }

            _bs.DataSource = _rows;
            grid.DataSource = _bs;
        }

        private void ApplyFilters(int? focusId = null)
        {
            string q = (txtBuscar.Text ?? "").Trim().ToLowerInvariant();
            string dept = cmbDept.SelectedItem?.ToString() ?? "Todos los departamentos";
            string estado = cmbEstado.SelectedItem?.ToString() ?? "Todos los estados";

            IEnumerable<Employee> data = _rows;

            if (!string.IsNullOrEmpty(q))
            {
                data = data.Where(e =>
                    (e.Name ?? "").ToLower().Contains(q) ||
                    (e.Email ?? "").ToLower().Contains(q) ||
                    (e.Phone ?? "").ToLower().Contains(q) ||
                    (e.Position ?? "").ToLower().Contains(q) ||
                    (e.Department ?? "").ToLower().Contains(q));
            }

            if (dept != "Todos los departamentos")
                data = data.Where(e => string.Equals(e.Department, dept, StringComparison.OrdinalIgnoreCase));

            if (estado != "Todos los estados")
                data = data.Where(e => string.Equals(e.Status, estado, StringComparison.OrdinalIgnoreCase));

            var filtered = data.ToList();

            _bs.DataSource = new BindingList<Employee>(filtered);
            grid.DataSource = _bs;
            UpdateStatusLabel(filtered.Count);

            int? targetId = focusId ?? _lastKnownSelectionId;
            if (targetId.HasValue)
            {
                SelectEmployee(targetId.Value);
            }
            else if (grid.CurrentRow != null)
            {
                grid.ClearSelection();
                grid.CurrentCell = null;
            }

            AdjustGridWidth();
            UpdateActionStates();
        }

        private void NewEmployee()
        {
            using var dlg = new EmployeeDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                int? newId = null;
                try
                {
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand(@"
INSERT INTO dbo.Employees(Name,Email,Phone,Department,Position,Status,Salary,HireDate)
OUTPUT INSERTED.Id
VALUES(@n,@e,@p,@d,@po,@s,@sa,@h);", cn);
                    cmd.Parameters.AddWithValue("@n", dlg.Result.Name);
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(dlg.Result.Email) ? (object)DBNull.Value : dlg.Result.Email);
                    cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(dlg.Result.Phone) ? (object)DBNull.Value : dlg.Result.Phone);
                    cmd.Parameters.AddWithValue("@d", dlg.Result.Department ?? "");
                    cmd.Parameters.AddWithValue("@po", dlg.Result.Position ?? "");
                    cmd.Parameters.AddWithValue("@s", dlg.Result.Status ?? "Activo");
                    cmd.Parameters.AddWithValue("@sa", dlg.Result.Salary);
                    cmd.Parameters.AddWithValue("@h", dlg.Result.HireDate.HasValue ? dlg.Result.HireDate.Value.Date : (object)DBNull.Value);
                    var result = cmd.ExecuteScalar();
                    if (result is int id)
                    {
                        newId = id;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error guardando empleado:\n" + ex.Message);
                }

                DataEvents.PublishEmpleadosChanged(newId);
            }
        }

        private void EditSelected()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.DataBoundItem is not Employee cur)
            {
                MessageBox.Show("Selecciona un empleado.", "Empleados");
                return;
            }

            using var dlg = new EmployeeDialog(cur);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                try
                {
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand(@"
UPDATE dbo.Employees SET
 Name=@n, Email=@e, Phone=@p, Department=@d, Position=@po, Status=@s, Salary=@sa, HireDate=@h
WHERE Id=@id;", cn);
                    cmd.Parameters.AddWithValue("@id", cur.Id);
                    cmd.Parameters.AddWithValue("@n", dlg.Result.Name);
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(dlg.Result.Email) ? (object)DBNull.Value : dlg.Result.Email);
                    cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(dlg.Result.Phone) ? (object)DBNull.Value : dlg.Result.Phone);
                    cmd.Parameters.AddWithValue("@d", dlg.Result.Department ?? "");
                    cmd.Parameters.AddWithValue("@po", dlg.Result.Position ?? "");
                    cmd.Parameters.AddWithValue("@s", dlg.Result.Status ?? "Activo");
                    cmd.Parameters.AddWithValue("@sa", dlg.Result.Salary);
                    cmd.Parameters.AddWithValue("@h", dlg.Result.HireDate.HasValue ? dlg.Result.HireDate.Value.Date : (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error actualizando empleado:\n" + ex.Message);
                }
                DataEvents.PublishEmpleadosChanged(cur.Id);
            }
        }

        private void DeleteSelected()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.DataBoundItem is not Employee cur)
            {
                MessageBox.Show("Selecciona un empleado.", "Empleados");
                return;
            }

            if (MessageBox.Show($"¿Eliminar a \"{cur.Name}\"?", "Empleados", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand("DELETE FROM dbo.Employees WHERE Id=@id;", cn);
                cmd.Parameters.AddWithValue("@id", cur.Id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error eliminando empleado:\n" + ex.Message, "Empleados", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataEvents.PublishEmpleadosChanged();
        }

        // ===== detalle desplegable =====
        private void PaintDetailFromSelection()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.DataBoundItem is not Employee e)
            {
                lblDNombre.Text = "-";
                lblDCargo.Text = "-";
                lblDEstado.Text = "-";
                lblDEmail.Text = "-";
                lblDPhone.Text = "-";
                lblDDept.Text = "-";
                lblDIngreso.Text = "-";
                lblDSalario.Text = "$0";
                return;
            }

            lblDNombre.Text = e.Name;
            lblDCargo.Text = e.Position;
            lblDEstado.Text = e.Status;
            lblDEmail.Text = e.Email;
            lblDPhone.Text = e.Phone;
            lblDDept.Text = e.Department;
            lblDIngreso.Text = e.HireDate?.ToString("dd/MM/yyyy") ?? "-";
            lblDSalario.Text = e.Salary.ToString("C0");
        }

        private void ToggleDetalle()
        {
            _detalleVisible = !_detalleVisible;
            contentSplit.Panel2Collapsed = !_detalleVisible;
            btnToggleDetalle.Text = _detalleVisible ? "◀ Ocultar detalle" : "▶ Mostrar detalle";
        }

        private void AdjustGridWidth()
        {
            if (!_detalleVisible)
            {
                contentSplit.Panel2Collapsed = true;
            }
            else
            {
                contentSplit.Panel2Collapsed = false;
            }
        }

        private void UpdateDepartmentFilter(IEnumerable<Employee> data, string? previousSelection)
        {
            var depts = new List<string> { "Todos los departamentos" };
            depts.AddRange(data
                .Select(r => r.Department)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));

            var list = new BindingList<string>(depts);
            cmbDept.DataSource = list;

            if (!string.IsNullOrWhiteSpace(previousSelection) && depts.Contains(previousSelection))
            {
                cmbDept.SelectedItem = previousSelection;
            }
        }

        private void RestoreEstadoSelection(string? previousSelection)
        {
            if (!string.IsNullOrWhiteSpace(previousSelection))
            {
                for (int i = 0; i < cmbEstado.Items.Count; i++)
                {
                    if (string.Equals(cmbEstado.Items[i]?.ToString(), previousSelection, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbEstado.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        private void RestoreSort(string? columnName, WinFormsSortOrder sortOrder)
        {
            if (string.IsNullOrWhiteSpace(columnName) || sortOrder == WinFormsSortOrder.None)
            {
                return;
            }

            try
            {
                var column = grid.Columns
                    .Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => string.Equals(c.DataPropertyName, columnName, StringComparison.OrdinalIgnoreCase));

                if (column != null)
                {
                    var direction = sortOrder == WinFormsSortOrder.Descending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;

                    grid.Sort(column, direction);
                }
            }
            catch (NotSupportedException)
            {
                // El origen de datos no soporta ordenamiento; ignorar.
            }
        }

        private void SelectEmployee(int employeeId)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is Employee e && e.Id == employeeId)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0)
                    {
                        grid.CurrentCell = row.Cells[0];
                    }

                    int index = Math.Max(0, row.Index - 2);
                    grid.FirstDisplayedScrollingRowIndex = index;
                    _lastKnownSelectionId = employeeId;
                    return;
                }
            }

            _lastKnownSelectionId = null;
        }

        private void RememberSelection()
        {
            _lastKnownSelectionId = GetSelectedEmployeeId();
        }

        private int? GetSelectedEmployeeId()
        {
            return grid.CurrentRow?.DataBoundItem is Employee e ? e.Id : null;
        }

        private void UpdateActionStates()
        {
            bool hasSelection = grid.CurrentRow?.DataBoundItem is Employee;
            btnNuevo.Enabled = !_isLoading;
            btnEditar.Enabled = !_isLoading && hasSelection;
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
            lblStatus.Text = _isLoading ? "Actualizando empleados…" : $"Empleados: {count}";
        }

        private void DisposeSubscriptions()
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;

            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
            _subscriptions.Clear();
        }
    }

    // ====== Modelo ======
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
        public string Status { get; set; } = "Activo";
        public decimal Salary { get; set; }
        public DateTime? HireDate { get; set; }
    }

    // ====== Diálogo de Empleado ======
    public class EmployeeDialog : Form
    {
        public Employee Result { get; private set; }

        private TextBox txtName;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private ComboBox cmbDepartment;
        private TextBox txtPosition;
        private ComboBox cmbStatus;
        private NumericUpDown numSalary;
        private DateTimePicker dtpHireDate;
        private CheckBox chkNoHireDate;
        private Button btnGuardar;
        private Button btnCancelar;

        public EmployeeDialog(Employee existing = null)
        {
            this.Text = existing == null ? "Nuevo Empleado" : "Editar Empleado";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new System.Drawing.Size(450, 450);

            BuildForm();

            // Cargar datos si es edición
            if (existing != null)
            {
                txtName.Text = existing.Name;
                txtEmail.Text = existing.Email;
                txtPhone.Text = existing.Phone;
                cmbDepartment.Text = existing.Department;
                txtPosition.Text = existing.Position;
                cmbStatus.Text = existing.Status;
                numSalary.Value = existing.Salary;

                if (existing.HireDate.HasValue)
                {
                    dtpHireDate.Value = existing.HireDate.Value;
                    chkNoHireDate.Checked = false;
                }
                else
                {
                    chkNoHireDate.Checked = true;
                    dtpHireDate.Enabled = false;
                }

                Result = existing;
            }
        }

        private void BuildForm()
        {
            int y = 20;
            int lblWidth = 120;
            int ctrlX = lblWidth + 30;
            int ctrlWidth = 250;

            // Nombre
            AddLabel("Nombre:", 20, y);
            txtName = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Email
            AddLabel("Email:", 20, y);
            txtEmail = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Teléfono
            AddLabel("Teléfono:", 20, y);
            txtPhone = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Departamento
            AddLabel("Departamento:", 20, y);
            cmbDepartment = AddComboBox(ctrlX, y, ctrlWidth);
            cmbDepartment.Items.AddRange(new object[] {
                "Ventas", "Compras", "Inventario", "Contabilidad",
                "Recursos Humanos", "Sistemas", "Administración"
            });
            y += 35;

            // Cargo
            AddLabel("Cargo:", 20, y);
            txtPosition = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Estado
            AddLabel("Estado:", 20, y);
            cmbStatus = AddComboBox(ctrlX, y, ctrlWidth);
            cmbStatus.Items.AddRange(new object[] { "Activo", "Vacaciones", "Inactivo" });
            cmbStatus.SelectedIndex = 0;
            y += 35;

            // Salario
            AddLabel("Salario:", 20, y);
            numSalary = new NumericUpDown
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Maximum = 99999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            this.Controls.Add(numSalary);
            y += 35;

            // Fecha de ingreso
            AddLabel("Fecha de ingreso:", 20, y);
            dtpHireDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Format = DateTimePickerFormat.Short
            };
            this.Controls.Add(dtpHireDate);
            y += 30;

            chkNoHireDate = new CheckBox
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Text = "Sin fecha de ingreso"
            };
            chkNoHireDate.CheckedChanged += (s, e) =>
            {
                dtpHireDate.Enabled = !chkNoHireDate.Checked;
            };
            this.Controls.Add(chkNoHireDate);
            y += 40;

            // Botones
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new System.Drawing.Point(ctrlX, y),
                Width = 100,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancelar);

            btnGuardar = new Button
            {
                Text = "Guardar",
                Location = new System.Drawing.Point(ctrlX + 110, y),
                Width = 100,
                DialogResult = DialogResult.OK
            };
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);

            this.CancelButton = btnCancelar;
            this.AcceptButton = btnGuardar;
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y + 3),
                Width = 120,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new System.Drawing.Point(x, y),
                Width = width
            };
            this.Controls.Add(txt);
            return txt;
        }

        private ComboBox AddComboBox(int x, int y, int width)
        {
            var cmb = new ComboBox
            {
                Location = new System.Drawing.Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            this.Controls.Add(cmb);
            return cmb;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (Result == null)
                Result = new Employee();

            Result.Name = txtName.Text.Trim();
            Result.Email = txtEmail.Text.Trim();
            Result.Phone = txtPhone.Text.Trim();
            Result.Department = cmbDepartment.Text.Trim();
            Result.Position = txtPosition.Text.Trim();
            Result.Status = cmbStatus.Text;
            Result.Salary = numSalary.Value;
            Result.HireDate = chkNoHireDate.Checked ? (DateTime?)null : dtpHireDate.Value.Date;
        }
    }
}