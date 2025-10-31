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
        private const string PaymentTypeNomina = "Nomina";
        private const string PaymentTypeLiquidacion = "Liquidacion";

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
            btnNomina.Click += (_, __) => RegisterPayroll();
            btnLiquidacion.Click += (_, __) => RegisterLiquidation();
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
            using var cmd = new SqlCommand(@"
SELECT e.Id,
       e.Name,
        e.Email,
        e.Phone,
        e.Department,
        e.Position,
        e.Status,
        e.Salary,
        e.HireDate,
        e.DocumentNumber,
        e.Address,
        e.BankAccount,
        e.EmergencyContact,
        e.EmergencyPhone,
        e.BirthDate,
        e.Gender,
        e.MaritalStatus,
        e.HealthProvider,
        e.PensionProvider,
        e.BloodType,
        e.ContractType,
        e.CompensationFund,
        e.ContractDuration,
        p.PeriodStart AS PayrollStart,
        p.PeriodEnd AS PayrollEnd,
        p.Amount AS PayrollAmount,
        p.Notes AS PayrollNotes,
        l.PeriodStart AS LiquidationStart,
        l.PeriodEnd AS LiquidationEnd,
        l.Amount AS LiquidationAmount,
        l.Notes AS LiquidationNotes
FROM dbo.Employees e
OUTER APPLY (
    SELECT TOP 1 PeriodStart, PeriodEnd, Amount, Notes
    FROM dbo.EmployeePayments
    WHERE EmployeeId = e.Id AND Type = @tNomina
    ORDER BY PeriodEnd DESC, CreatedAt DESC, Id DESC
) p
OUTER APPLY (
    SELECT TOP 1 PeriodStart, PeriodEnd, Amount, Notes
    FROM dbo.EmployeePayments
    WHERE EmployeeId = e.Id AND Type = @tLiquidacion
    ORDER BY PeriodEnd DESC, CreatedAt DESC, Id DESC
) l
ORDER BY e.Name ASC;", cn);
            cmd.Parameters.AddWithValue("@tNomina", PaymentTypeNomina);
            cmd.Parameters.AddWithValue("@tLiquidacion", PaymentTypeLiquidacion);
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
                    HireDate = rd.IsDBNull(8) ? (DateTime?)null : rd.GetDateTime(8),
                    DocumentNumber = rd.IsDBNull(9) ? "" : rd.GetString(9),
                    Address = rd.IsDBNull(10) ? "" : rd.GetString(10),
                    BankAccount = rd.IsDBNull(11) ? "" : rd.GetString(11),
                    EmergencyContact = rd.IsDBNull(12) ? "" : rd.GetString(12),
                    EmergencyPhone = rd.IsDBNull(13) ? "" : rd.GetString(13),
                    BirthDate = rd.IsDBNull(14) ? (DateTime?)null : rd.GetDateTime(14),
                    Gender = rd.IsDBNull(15) ? "" : rd.GetString(15),
                    MaritalStatus = rd.IsDBNull(16) ? "" : rd.GetString(16),
                    HealthProvider = rd.IsDBNull(17) ? "" : rd.GetString(17),
                    PensionProvider = rd.IsDBNull(18) ? "" : rd.GetString(18),
                    BloodType = rd.IsDBNull(19) ? "" : rd.GetString(19),
                    ContractType = rd.IsDBNull(20) ? "" : rd.GetString(20),
                    CompensationFund = rd.IsDBNull(21) ? "" : rd.GetString(21),
                    ContractDuration = rd.IsDBNull(22) ? "" : rd.GetString(22),
                    LastPayrollPeriodStart = rd.IsDBNull(23) ? (DateTime?)null : rd.GetDateTime(23),
                    LastPayrollPeriodEnd = rd.IsDBNull(24) ? (DateTime?)null : rd.GetDateTime(24),
                    LastPayrollAmount = rd.IsDBNull(25) ? (decimal?)null : rd.GetDecimal(25),
                    LastPayrollNotes = rd.IsDBNull(26) ? "" : rd.GetString(26),
                    LastLiquidationPeriodStart = rd.IsDBNull(27) ? (DateTime?)null : rd.GetDateTime(27),
                    LastLiquidationPeriodEnd = rd.IsDBNull(28) ? (DateTime?)null : rd.GetDateTime(28),
                    LastLiquidationAmount = rd.IsDBNull(29) ? (decimal?)null : rd.GetDecimal(29),
                    LastLiquidationNotes = rd.IsDBNull(30) ? "" : rd.GetString(30)
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
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Documento", DataPropertyName = "DocumentNumber", Width = 140 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Departamento", DataPropertyName = "Department", Width = 140 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cargo", DataPropertyName = "Position", Width = 160 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Status", Width = 100 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Salario", DataPropertyName = "Salary", Width = 120, DefaultCellStyle = { Format = "C2" } });
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ingreso", DataPropertyName = "HireDate", Width = 100, DefaultCellStyle = { Format = "dd/MM/yyyy" } });
            }

            _bs.DataSource = _rows;
            grid.DataSource = _bs;
        }

        private void ApplyFilters(int? focusId = null)
        {
            string rawQuery = (txtBuscar.Text ?? "").Trim();
            string q = rawQuery.ToLowerInvariant();
            string dept = cmbDept.SelectedItem?.ToString() ?? "Todos los departamentos";
            string estado = cmbEstado.SelectedItem?.ToString() ?? "Todos los estados";

            IEnumerable<Employee> data = _rows;

            if (!string.IsNullOrEmpty(q))
            {
                data = data.Where(e =>
                    (e.Name ?? "").ToLower().Contains(q) ||
                    (e.Email ?? "").ToLower().Contains(q) ||
                    (e.Phone ?? "").ToLower().Contains(q) ||
                    (e.DocumentNumber ?? "").ToLower().Contains(q) ||
                    (e.Address ?? "").ToLower().Contains(q) ||
                    (e.BankAccount ?? "").ToLower().Contains(q) ||
                    (e.EmergencyContact ?? "").ToLower().Contains(q) ||
                    (e.EmergencyPhone ?? "").ToLower().Contains(q) ||
                    (e.Position ?? "").ToLower().Contains(q) ||
                    (e.Department ?? "").ToLower().Contains(q) ||
                    (e.Gender ?? "").ToLower().Contains(q) ||
                    (e.MaritalStatus ?? "").ToLower().Contains(q) ||
                    (e.HealthProvider ?? "").ToLower().Contains(q) ||
                    (e.PensionProvider ?? "").ToLower().Contains(q) ||
                    (e.BloodType ?? "").ToLower().Contains(q) ||
                    (e.ContractType ?? "").ToLower().Contains(q) ||
                    (e.CompensationFund ?? "").ToLower().Contains(q) ||
                    (e.ContractDuration ?? "").ToLower().Contains(q) ||
                    (e.BirthDate.HasValue && e.BirthDate.Value.ToString("dd/MM/yyyy").ToLower().Contains(q)));
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
INSERT INTO dbo.Employees(Name,Email,Phone,Department,Position,Status,Salary,HireDate,DocumentNumber,Address,BankAccount,EmergencyContact,EmergencyPhone,BirthDate,Gender,MaritalStatus,HealthProvider,PensionProvider,BloodType,ContractType,CompensationFund,ContractDuration)
OUTPUT INSERTED.Id
VALUES(@n,@e,@p,@d,@po,@s,@sa,@h,@doc,@addr,@bank,@emc,@emp,@birth,@gender,@marital,@health,@pension,@blood,@contractType,@compFund,@contractDuration);", cn);
                    cmd.Parameters.AddWithValue("@n", dlg.Result.Name);
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(dlg.Result.Email) ? (object)DBNull.Value : dlg.Result.Email);
                    cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(dlg.Result.Phone) ? (object)DBNull.Value : dlg.Result.Phone);
                    cmd.Parameters.AddWithValue("@d", dlg.Result.Department ?? "");
                    cmd.Parameters.AddWithValue("@po", dlg.Result.Position ?? "");
                    cmd.Parameters.AddWithValue("@s", dlg.Result.Status ?? "Activo");
                    cmd.Parameters.AddWithValue("@sa", dlg.Result.Salary);
                    cmd.Parameters.AddWithValue("@h", dlg.Result.HireDate.HasValue ? dlg.Result.HireDate.Value.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@doc", string.IsNullOrWhiteSpace(dlg.Result.DocumentNumber) ? (object)DBNull.Value : dlg.Result.DocumentNumber);
                    cmd.Parameters.AddWithValue("@addr", string.IsNullOrWhiteSpace(dlg.Result.Address) ? (object)DBNull.Value : dlg.Result.Address);
                    cmd.Parameters.AddWithValue("@bank", string.IsNullOrWhiteSpace(dlg.Result.BankAccount) ? (object)DBNull.Value : dlg.Result.BankAccount);
                    cmd.Parameters.AddWithValue("@emc", string.IsNullOrWhiteSpace(dlg.Result.EmergencyContact) ? (object)DBNull.Value : dlg.Result.EmergencyContact);
                    cmd.Parameters.AddWithValue("@emp", string.IsNullOrWhiteSpace(dlg.Result.EmergencyPhone) ? (object)DBNull.Value : dlg.Result.EmergencyPhone);
                    cmd.Parameters.AddWithValue("@birth", dlg.Result.BirthDate.HasValue ? dlg.Result.BirthDate.Value.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(dlg.Result.Gender) ? (object)DBNull.Value : dlg.Result.Gender);
                    cmd.Parameters.AddWithValue("@marital", string.IsNullOrWhiteSpace(dlg.Result.MaritalStatus) ? (object)DBNull.Value : dlg.Result.MaritalStatus);
                    cmd.Parameters.AddWithValue("@health", string.IsNullOrWhiteSpace(dlg.Result.HealthProvider) ? (object)DBNull.Value : dlg.Result.HealthProvider);
                    cmd.Parameters.AddWithValue("@pension", string.IsNullOrWhiteSpace(dlg.Result.PensionProvider) ? (object)DBNull.Value : dlg.Result.PensionProvider);
                    cmd.Parameters.AddWithValue("@blood", string.IsNullOrWhiteSpace(dlg.Result.BloodType) ? (object)DBNull.Value : dlg.Result.BloodType);
                    cmd.Parameters.AddWithValue("@contractType", string.IsNullOrWhiteSpace(dlg.Result.ContractType) ? (object)DBNull.Value : dlg.Result.ContractType);
                    cmd.Parameters.AddWithValue("@compFund", string.IsNullOrWhiteSpace(dlg.Result.CompensationFund) ? (object)DBNull.Value : dlg.Result.CompensationFund);
                    cmd.Parameters.AddWithValue("@contractDuration", string.IsNullOrWhiteSpace(dlg.Result.ContractDuration) ? (object)DBNull.Value : dlg.Result.ContractDuration);
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
 Name=@n, Email=@e, Phone=@p, Department=@d, Position=@po, Status=@s, Salary=@sa, HireDate=@h,
 DocumentNumber=@doc, Address=@addr, BankAccount=@bank, EmergencyContact=@emc, EmergencyPhone=@emp,
 BirthDate=@birth, Gender=@gender, MaritalStatus=@marital, HealthProvider=@health,
 PensionProvider=@pension, BloodType=@blood, ContractType=@contractType, CompensationFund=@compFund,
 ContractDuration=@contractDuration
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
                    cmd.Parameters.AddWithValue("@doc", string.IsNullOrWhiteSpace(dlg.Result.DocumentNumber) ? (object)DBNull.Value : dlg.Result.DocumentNumber);
                    cmd.Parameters.AddWithValue("@addr", string.IsNullOrWhiteSpace(dlg.Result.Address) ? (object)DBNull.Value : dlg.Result.Address);
                    cmd.Parameters.AddWithValue("@bank", string.IsNullOrWhiteSpace(dlg.Result.BankAccount) ? (object)DBNull.Value : dlg.Result.BankAccount);
                    cmd.Parameters.AddWithValue("@emc", string.IsNullOrWhiteSpace(dlg.Result.EmergencyContact) ? (object)DBNull.Value : dlg.Result.EmergencyContact);
                    cmd.Parameters.AddWithValue("@emp", string.IsNullOrWhiteSpace(dlg.Result.EmergencyPhone) ? (object)DBNull.Value : dlg.Result.EmergencyPhone);
                    cmd.Parameters.AddWithValue("@birth", dlg.Result.BirthDate.HasValue ? dlg.Result.BirthDate.Value.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(dlg.Result.Gender) ? (object)DBNull.Value : dlg.Result.Gender);
                    cmd.Parameters.AddWithValue("@marital", string.IsNullOrWhiteSpace(dlg.Result.MaritalStatus) ? (object)DBNull.Value : dlg.Result.MaritalStatus);
                    cmd.Parameters.AddWithValue("@health", string.IsNullOrWhiteSpace(dlg.Result.HealthProvider) ? (object)DBNull.Value : dlg.Result.HealthProvider);
                    cmd.Parameters.AddWithValue("@pension", string.IsNullOrWhiteSpace(dlg.Result.PensionProvider) ? (object)DBNull.Value : dlg.Result.PensionProvider);
                    cmd.Parameters.AddWithValue("@blood", string.IsNullOrWhiteSpace(dlg.Result.BloodType) ? (object)DBNull.Value : dlg.Result.BloodType);
                    cmd.Parameters.AddWithValue("@contractType", string.IsNullOrWhiteSpace(dlg.Result.ContractType) ? (object)DBNull.Value : dlg.Result.ContractType);
                    cmd.Parameters.AddWithValue("@compFund", string.IsNullOrWhiteSpace(dlg.Result.CompensationFund) ? (object)DBNull.Value : dlg.Result.CompensationFund);
                    cmd.Parameters.AddWithValue("@contractDuration", string.IsNullOrWhiteSpace(dlg.Result.ContractDuration) ? (object)DBNull.Value : dlg.Result.ContractDuration);
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

        private void RegisterPayroll()
        {
            RegisterPayment(PaymentTypeNomina, "nómina");
        }

        private void RegisterLiquidation()
        {
            RegisterPayment(PaymentTypeLiquidacion, "liquidación");
        }

        private void RegisterPayment(string type, string friendlyName)
        {
            if (!TryGetSelectedEmployee(out var employee) || employee == null)
            {
                MessageBox.Show("Selecciona un empleado.", "Empleados");
                return;
            }

            using var dlg = new EmployeePaymentDialog(employee, friendlyName, type == PaymentTypeNomina);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                try
                {
                    using var cn = Database.OpenConnection();
                    using var cmd = new SqlCommand(@"
INSERT INTO dbo.EmployeePayments(EmployeeId,Type,PeriodStart,PeriodEnd,Amount,Notes)
VALUES(@id,@t,@ps,@pe,@a,@n);", cn);
                    cmd.Parameters.AddWithValue("@id", employee.Id);
                    cmd.Parameters.AddWithValue("@t", type);
                    cmd.Parameters.AddWithValue("@ps", dlg.Result.PeriodStart.HasValue ? dlg.Result.PeriodStart.Value.Date : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pe", dlg.Result.PeriodEnd.Date);
                    cmd.Parameters.AddWithValue("@a", dlg.Result.Amount);
                    cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(dlg.Result.Notes) ? (object)DBNull.Value : dlg.Result.Notes.Trim());
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error registrando {friendlyName}:\n" + ex.Message, "Empleados", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DataEvents.PublishEmpleadosChanged(employee.Id);
            }
        }

        private bool TryGetSelectedEmployee(out Employee? employee)
        {
            if (grid.CurrentRow?.DataBoundItem is Employee e)
            {
                employee = e;
                return true;
            }

            employee = null;
            return false;
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
                lblDDocumento.Text = "-";
                lblDDireccion.Text = "-";
                lblDBanco.Text = "-";
                lblDNacimiento.Text = "-";
                lblDGenero.Text = "-";
                lblDEstadoCivil.Text = "-";
                lblDSalud.Text = "-";
                lblDPension.Text = "-";
                lblDTipoSangre.Text = "-";
                lblDTipoContrato.Text = "-";
                lblDCompensacion.Text = "-";
                lblDDuracionContrato.Text = "-";
                lblDContactoEmergencia.Text = "-";
                lblDTelefonoEmergencia.Text = "-";
                lblDUltimaNomina.Text = "-";
                lblDUltimaLiquidacion.Text = "-";
                return;
            }

            lblDNombre.Text = e.Name;
            lblDCargo.Text = e.Position;
            lblDEstado.Text = e.Status;
            lblDEmail.Text = e.Email;
            lblDPhone.Text = e.Phone;
            lblDDept.Text = e.Department;
            lblDIngreso.Text = e.HireDate?.ToString("dd/MM/yyyy") ?? "-";
            lblDSalario.Text = e.Salary.ToString("C2");
            lblDDocumento.Text = string.IsNullOrWhiteSpace(e.DocumentNumber) ? "-" : e.DocumentNumber;
            lblDDireccion.Text = string.IsNullOrWhiteSpace(e.Address) ? "-" : e.Address;
            lblDBanco.Text = string.IsNullOrWhiteSpace(e.BankAccount) ? "-" : e.BankAccount;
            lblDNacimiento.Text = e.BirthDate?.ToString("dd/MM/yyyy") ?? "-";
            lblDGenero.Text = string.IsNullOrWhiteSpace(e.Gender) ? "-" : e.Gender;
            lblDEstadoCivil.Text = string.IsNullOrWhiteSpace(e.MaritalStatus) ? "-" : e.MaritalStatus;
            lblDSalud.Text = string.IsNullOrWhiteSpace(e.HealthProvider) ? "-" : e.HealthProvider;
            lblDPension.Text = string.IsNullOrWhiteSpace(e.PensionProvider) ? "-" : e.PensionProvider;
            lblDTipoSangre.Text = string.IsNullOrWhiteSpace(e.BloodType) ? "-" : e.BloodType;
            lblDTipoContrato.Text = string.IsNullOrWhiteSpace(e.ContractType) ? "-" : e.ContractType;
            lblDCompensacion.Text = string.IsNullOrWhiteSpace(e.CompensationFund) ? "-" : e.CompensationFund;
            lblDDuracionContrato.Text = string.IsNullOrWhiteSpace(e.ContractDuration) ? "-" : e.ContractDuration;
            lblDContactoEmergencia.Text = string.IsNullOrWhiteSpace(e.EmergencyContact) ? "-" : e.EmergencyContact;
            lblDTelefonoEmergencia.Text = string.IsNullOrWhiteSpace(e.EmergencyPhone) ? "-" : e.EmergencyPhone;
            lblDUltimaNomina.Text = FormatPaymentDetail(e.LastPayrollPeriodStart, e.LastPayrollPeriodEnd, e.LastPayrollAmount, e.LastPayrollNotes);
            lblDUltimaLiquidacion.Text = FormatPaymentDetail(e.LastLiquidationPeriodStart, e.LastLiquidationPeriodEnd, e.LastLiquidationAmount, e.LastLiquidationNotes);
        }

        private static string FormatPaymentDetail(DateTime? start, DateTime? end, decimal? amount, string notes)
        {
            if (!start.HasValue && !end.HasValue && !amount.HasValue && string.IsNullOrWhiteSpace(notes))
            {
                return "Sin registros";
            }

            string period = end.HasValue
                ? start.HasValue ? $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}" : end.Value.ToString("dd/MM/yyyy")
                : start.HasValue ? start.Value.ToString("dd/MM/yyyy") : "-";

            string amountText = amount.HasValue ? amount.Value.ToString("C2") : "$0.00";

            string result = $"{period} · {amountText}";

            if (!string.IsNullOrWhiteSpace(notes))
            {
                result += $" · {notes}";
            }

            return result;
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
            btnNomina.Enabled = !_isLoading && hasSelection;
            btnLiquidacion.Enabled = !_isLoading && hasSelection;
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
        public string DocumentNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string BankAccount { get; set; } = "";
        public string EmergencyContact { get; set; } = "";
        public string EmergencyPhone { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; } = "";
        public string MaritalStatus { get; set; } = "";
        public string HealthProvider { get; set; } = "";
        public string PensionProvider { get; set; } = "";
        public string BloodType { get; set; } = "";
        public string ContractType { get; set; } = "";
        public string CompensationFund { get; set; } = "";
        public string ContractDuration { get; set; } = "";
        public DateTime? LastPayrollPeriodStart { get; set; }
        public DateTime? LastPayrollPeriodEnd { get; set; }
        public decimal? LastPayrollAmount { get; set; }
        public string LastPayrollNotes { get; set; } = "";
        public DateTime? LastLiquidationPeriodStart { get; set; }
        public DateTime? LastLiquidationPeriodEnd { get; set; }
        public decimal? LastLiquidationAmount { get; set; }
        public string LastLiquidationNotes { get; set; } = "";
    }

    // ====== Diálogo de Empleado ======
    public class EmployeeDialog : Form
    {
        public Employee Result { get; private set; }

        private Panel pnlContent;
        private TextBox txtName;
        private TextBox txtDocument;
        private DateTimePicker dtpBirthDate;
        private CheckBox chkNoBirthDate;
        private ComboBox cmbGender;
        private ComboBox cmbMaritalStatus;
        private ComboBox cmbContractType;
        private TextBox txtCompensationFund;
        private TextBox txtContractDuration;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private TextBox txtAddress;
        private ComboBox cmbDepartment;
        private TextBox txtPosition;
        private ComboBox cmbStatus;
        private NumericUpDown numSalary;
        private TextBox txtBankAccount;
        private TextBox txtHealthProvider;
        private TextBox txtPensionProvider;
        private TextBox txtBloodType;
        private TextBox txtEmergencyContact;
        private TextBox txtEmergencyPhone;
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
            this.Size = new System.Drawing.Size(480, 780);

            BuildForm();

            chkNoBirthDate.Checked = true;
            dtpBirthDate.Enabled = false;

            // Cargar datos si es edición
            if (existing != null)
            {
                txtName.Text = existing.Name;
                txtDocument.Text = existing.DocumentNumber;
                if (existing.BirthDate.HasValue)
                {
                    dtpBirthDate.Value = existing.BirthDate.Value;
                    chkNoBirthDate.Checked = false;
                }
                else
                {
                    chkNoBirthDate.Checked = true;
                    dtpBirthDate.Enabled = false;
                }
                cmbGender.Text = existing.Gender;
                cmbMaritalStatus.Text = existing.MaritalStatus;
                cmbContractType.Text = existing.ContractType;
                txtCompensationFund.Text = existing.CompensationFund;
                txtContractDuration.Text = existing.ContractDuration;
                txtEmail.Text = existing.Email;
                txtPhone.Text = existing.Phone;
                txtAddress.Text = existing.Address;
                cmbDepartment.Text = existing.Department;
                txtPosition.Text = existing.Position;
                cmbStatus.Text = existing.Status;
                numSalary.Value = existing.Salary;
                txtBankAccount.Text = existing.BankAccount;
                txtHealthProvider.Text = existing.HealthProvider;
                txtPensionProvider.Text = existing.PensionProvider;
                txtBloodType.Text = existing.BloodType;
                txtEmergencyContact.Text = existing.EmergencyContact;
                txtEmergencyPhone.Text = existing.EmergencyPhone;

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
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            Controls.Add(pnlContent);

            int y = 20;
            int lblWidth = 120;
            int ctrlX = lblWidth + 30;
            int ctrlWidth = 250;

            // Nombre
            AddLabel("Nombre:", 20, y);
            txtName = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Documento
            AddLabel("Documento:", 20, y);
            txtDocument = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Fecha nacimiento
            AddLabel("Fecha nacimiento:", 20, y);
            dtpBirthDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Format = DateTimePickerFormat.Short
            };
            pnlContent.Controls.Add(dtpBirthDate);
            y += 30;

            chkNoBirthDate = new CheckBox
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Text = "Sin fecha de nacimiento"
            };
            chkNoBirthDate.CheckedChanged += (s, e) =>
            {
                dtpBirthDate.Enabled = !chkNoBirthDate.Checked;
            };
            pnlContent.Controls.Add(chkNoBirthDate);
            y += 35;

            // Género
            AddLabel("Género:", 20, y);
            cmbGender = AddComboBox(ctrlX, y, ctrlWidth);
            cmbGender.Items.AddRange(new object[]
            {
                "Femenino",
                "Masculino",
                "No binario",
                "Otro"
            });
            y += 35;

            // Estado civil
            AddLabel("Estado civil:", 20, y);
            cmbMaritalStatus = AddComboBox(ctrlX, y, ctrlWidth);
            cmbMaritalStatus.Items.AddRange(new object[]
            {
                "Soltero/a",
                "Casado/a",
                "Unión libre",
                "Divorciado/a",
                "Viudo/a"
            });
            y += 35;

            // Tipo de contrato
            AddLabel("Tipo de contrato:", 20, y);
            cmbContractType = AddComboBox(ctrlX, y, ctrlWidth);
            cmbContractType.Items.AddRange(new object[]
            {
                "Término indefinido",
                "Término fijo",
                "Obra o labor",
                "Aprendizaje",
                "Prestación de servicios"
            });
            y += 35;

            // Caja de compensación
            AddLabel("Caja de compensación:", 20, y);
            txtCompensationFund = AddTextBox(ctrlX, y, ctrlWidth);
            txtCompensationFund.PlaceholderText = "Ej: Colsubsidio";
            y += 35;

            // Duración de contrato
            AddLabel("Duración contrato:", 20, y);
            txtContractDuration = AddTextBox(ctrlX, y, ctrlWidth);
            txtContractDuration.PlaceholderText = "Ej: 6 meses";
            y += 35;

            // Email
            AddLabel("Email:", 20, y);
            txtEmail = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Teléfono
            AddLabel("Teléfono:", 20, y);
            txtPhone = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Dirección
            AddLabel("Dirección:", 20, y);
            txtAddress = AddTextBox(ctrlX, y, ctrlWidth);
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
            pnlContent.Controls.Add(numSalary);
            y += 35;

            // Cuenta bancaria
            AddLabel("Cuenta bancaria:", 20, y);
            txtBankAccount = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Salud
            AddLabel("Salud/EPS:", 20, y);
            txtHealthProvider = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Pensión
            AddLabel("Pensión/AFP:", 20, y);
            txtPensionProvider = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Tipo de sangre
            AddLabel("Tipo de sangre:", 20, y);
            txtBloodType = AddTextBox(ctrlX, y, ctrlWidth);
            txtBloodType.CharacterCasing = CharacterCasing.Upper;
            y += 35;

            // Contacto de emergencia
            AddLabel("Contacto emergencia:", 20, y);
            txtEmergencyContact = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Teléfono de emergencia
            AddLabel("Teléfono emergencia:", 20, y);
            txtEmergencyPhone = AddTextBox(ctrlX, y, ctrlWidth);
            y += 35;

            // Fecha de ingreso
            AddLabel("Fecha de ingreso:", 20, y);
            dtpHireDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Format = DateTimePickerFormat.Short
            };
            pnlContent.Controls.Add(dtpHireDate);
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
            pnlContent.Controls.Add(chkNoHireDate);
            y += 40;

            // Botones
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new System.Drawing.Point(ctrlX, y),
                Width = 100,
                DialogResult = DialogResult.Cancel
            };
            pnlContent.Controls.Add(btnCancelar);

            btnGuardar = new Button
            {
                Text = "Guardar",
                Location = new System.Drawing.Point(ctrlX + 110, y),
                Width = 100,
                DialogResult = DialogResult.OK
            };
            btnGuardar.Click += BtnGuardar_Click;
            pnlContent.Controls.Add(btnGuardar);

            this.CancelButton = btnCancelar;
            this.AcceptButton = btnGuardar;

            pnlContent.AutoScrollMinSize = new System.Drawing.Size(0, y + 120);
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
            pnlContent.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new System.Drawing.Point(x, y),
                Width = width
            };
            pnlContent.Controls.Add(txt);
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
            pnlContent.Controls.Add(cmb);
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
            Result.DocumentNumber = txtDocument.Text.Trim();
            Result.BirthDate = chkNoBirthDate.Checked ? (DateTime?)null : dtpBirthDate.Value.Date;
            Result.Gender = cmbGender.Text.Trim();
            Result.MaritalStatus = cmbMaritalStatus.Text.Trim();
            Result.ContractType = cmbContractType.Text.Trim();
            Result.CompensationFund = txtCompensationFund.Text.Trim();
            Result.ContractDuration = txtContractDuration.Text.Trim();
            Result.Email = txtEmail.Text.Trim();
            Result.Phone = txtPhone.Text.Trim();
            Result.Address = txtAddress.Text.Trim();
            Result.Department = cmbDepartment.Text.Trim();
            Result.Position = txtPosition.Text.Trim();
            Result.Status = cmbStatus.Text;
            Result.Salary = numSalary.Value;
            Result.BankAccount = txtBankAccount.Text.Trim();
            Result.HealthProvider = txtHealthProvider.Text.Trim();
            Result.PensionProvider = txtPensionProvider.Text.Trim();
            Result.BloodType = txtBloodType.Text.Trim().ToUpperInvariant();
            Result.EmergencyContact = txtEmergencyContact.Text.Trim();
            Result.EmergencyPhone = txtEmergencyPhone.Text.Trim();
            Result.HireDate = chkNoHireDate.Checked ? (DateTime?)null : dtpHireDate.Value.Date;
        }
    }

    public class EmployeePaymentResult
    {
        public DateTime? PeriodStart { get; init; }
        public DateTime PeriodEnd { get; init; }
        public decimal Amount { get; init; }
        public string Notes { get; init; } = "";
    }

    public class EmployeePaymentDialog : Form
    {
        public EmployeePaymentResult? Result { get; private set; }

        private readonly Employee _employee;
        private readonly bool _isPayroll;

        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private CheckBox chkNoStart;
        private NumericUpDown numAmount;
        private Panel pnlBreakdown;
        private TableLayoutPanel tblBreakdown;
        private Button btnGuardar;
        private Button btnCancelar;
        private bool _isUpdatingAmount;
        private bool _amountAuto = true;
        private bool _suppressSuggestion;
        private bool _updatingPayroll;
        private PayrollBreakdown? _currentPayroll;
        private LiquidationBreakdown? _currentLiquidation;
        private RiskOption[]? _riskOptions;

        private const int BreakdownMinHeight = 220;
        private const int BreakdownBottomMargin = 90;

        private TextBox? txtContrato;
        private TextBox? txtCajaCompensacion;
        private TextBox? txtHorasLegales;
        private NumericUpDown? numSalarioBase;
        private NumericUpDown? numAuxilioTransporte;
        private TextBox? txtAuxilioDetalle;
        private NumericUpDown? numDevengados;
        private NumericUpDown? numIbc;
        private NumericUpDown? numSaludTrabajador;
        private NumericUpDown? numPensionTrabajador;
        private NumericUpDown? numFsp;
        private TextBox? txtFspDetalle;
        private NumericUpDown? numTotalDeducciones;
        private NumericUpDown? numNeto;
        private NumericUpDown? numSaludEmpleador;
        private NumericUpDown? numPensionEmpleador;
        private ComboBox? cmbArlClase;
        private NumericUpDown? numArlEmpleador;
        private NumericUpDown? numCcf;
        private NumericUpDown? numSena;
        private NumericUpDown? numIcbf;
        private NumericUpDown? numTotalEmpleador;
        private TextBox? txtEmpleadorDetalle;

        private TextBox? txtLiqContrato;
        private TextBox? txtLiqCaja;
        private TextBox? txtLiqPeriodo;
        private TextBox? txtLiqSalarioDetalle;
        private NumericUpDown? numLiqSalarioPendiente;
        private NumericUpDown? numLiqCesantias;
        private NumericUpDown? numLiqIntereses;
        private NumericUpDown? numLiqPrima;
        private NumericUpDown? numLiqVacaciones;
        private NumericUpDown? numLiqAuxilio;
        private TextBox? txtLiqAuxilioNota;
        private NumericUpDown? numLiqTotal;
        private TextBox? txtLiqRecordatorio;

        public EmployeePaymentDialog(Employee employee, string friendlyName, bool isPayroll)
        {
            _employee = employee;
            _isPayroll = isPayroll;

            this.Text = "Registrar " + friendlyName;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new System.Drawing.Size(460, 560);

            BuildForm();
        }

        private void BuildForm()
        {
            int y = 20;
            int lblWidth = 130;
            int ctrlX = lblWidth + 30;
            int ctrlWidth = Math.Max(220, this.ClientSize.Width - ctrlX - 20);

            var lblTitle = new Label
            {
                Text = _employee.Name,
                Font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);
            y += 40;

            AddLabel("Periodo inicio:", 20, y);
            dtpStart = new DateTimePicker
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Format = DateTimePickerFormat.Short
            };
            this.Controls.Add(dtpStart);
            y += 35;

            AddLabel("Periodo fin:", 20, y);
            dtpEnd = new DateTimePicker
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Format = DateTimePickerFormat.Short
            };
            this.Controls.Add(dtpEnd);
            y += 35;

            chkNoStart = new CheckBox
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Text = "Sin periodo inicial"
            };
            chkNoStart.CheckedChanged += (_, __) =>
            {
                dtpStart.Enabled = !chkNoStart.Checked;
                if (_suppressSuggestion)
                {
                    return;
                }
                UpdateSuggestion();
            };
            this.Controls.Add(chkNoStart);
            y += 35;

            AddLabel("Monto:", 20, y);
            numAmount = new NumericUpDown
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Maximum = 999999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            numAmount.ValueChanged += (_, __) =>
            {
                if (_isUpdatingAmount)
                {
                    return;
                }
                _amountAuto = false;
            };
            this.Controls.Add(numAmount);
            y += 35;

            var breakdownHeight = Math.Max(BreakdownMinHeight, this.ClientSize.Height - y - BreakdownBottomMargin);
            pnlBreakdown = new Panel
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(this.ClientSize.Width - 40, breakdownHeight),
                AutoScroll = true,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(6, 0, 6, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tblBreakdown = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                Padding = new Padding(0)
            };
            tblBreakdown.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            tblBreakdown.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlBreakdown.Controls.Add(tblBreakdown);
            this.Controls.Add(pnlBreakdown);

            InitializeBreakdownFields();

            y = pnlBreakdown.Bottom + 20;

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

            this.ClientSizeChanged += (_, __) => AdjustBreakdownHeight();

            PositionButtons();

            dtpStart.ValueChanged += (_, __) =>
            {
                if (_suppressSuggestion)
                {
                    return;
                }
                UpdateSuggestion();
            };

            dtpEnd.ValueChanged += (_, __) =>
            {
                if (_suppressSuggestion)
                {
                    return;
                }
                UpdateSuggestion();
            };

            InitializeDefaults();

            AdjustBreakdownHeight();
        }

        private void InitializeDefaults()
        {
            var today = DateTime.Today;
            DateTime startDefault;
            DateTime endDefault;

            _suppressSuggestion = true;

            if (_isPayroll)
            {
                startDefault = new DateTime(today.Year, today.Month, 1);
                endDefault = startDefault.AddMonths(1).AddDays(-1);
                chkNoStart.Checked = false;
            }
            else
            {
                startDefault = _employee.HireDate ?? today;
                endDefault = today;
                chkNoStart.Checked = !_employee.HireDate.HasValue;
            }

            var startValue = startDefault < dtpStart.MinDate ? dtpStart.MinDate : startDefault;
            var endValue = endDefault < dtpEnd.MinDate ? dtpEnd.MinDate : endDefault;

            if (endValue < startValue && !chkNoStart.Checked)
            {
                endValue = startValue;
            }

            dtpStart.Value = startValue;
            dtpEnd.Value = endValue;
            dtpStart.Enabled = !chkNoStart.Checked;
            _suppressSuggestion = false;

            _amountAuto = true;
            UpdateSuggestion();
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y + 3),
                Width = 130,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            this.Controls.Add(lbl);
            return lbl;
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            if (numAmount.Value <= 0)
            {
                MessageBox.Show("El monto debe ser mayor a cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numAmount.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!chkNoStart.Checked && dtpEnd.Value.Date < dtpStart.Value.Date)
            {
                MessageBox.Show("La fecha final debe ser mayor o igual a la inicial.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpEnd.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            Result = new EmployeePaymentResult
            {
                PeriodStart = chkNoStart.Checked ? (DateTime?)null : dtpStart.Value.Date,
                PeriodEnd = dtpEnd.Value.Date,
                Amount = numAmount.Value,
                Notes = string.Empty
            };
        }

        private void UpdateSuggestion()
        {
            PaymentSuggestion suggestion;
            if (_isPayroll)
            {
                suggestion = PayrollEngine.BuildPayrollSuggestion(_employee);
            }
            else
            {
                var endDate = dtpEnd.Value.Date;
                DateTime startDate;
                if (chkNoStart.Checked)
                {
                    startDate = _employee.HireDate?.Date ?? endDate;
                }
                else
                {
                    startDate = dtpStart.Value.Date;
                }

                if (endDate < startDate)
                {
                    startDate = endDate;
                }

                suggestion = PayrollEngine.BuildLiquidationSuggestion(_employee, startDate, endDate);
            }

            ApplySuggestion(suggestion);
        }

        private void ApplySuggestion(PaymentSuggestion suggestion)
        {
            if (_amountAuto)
            {
                _isUpdatingAmount = true;
                var amount = suggestion.Amount;
                if (amount < numAmount.Minimum)
                {
                    amount = numAmount.Minimum;
                }
                if (amount > numAmount.Maximum)
                {
                    amount = numAmount.Maximum;
                }
                numAmount.Value = amount;
                _isUpdatingAmount = false;
            }

            if (_isPayroll)
            {
                RenderPayrollSuggestion(suggestion);
            }
            else
            {
                RenderLiquidationSuggestion(suggestion);
            }
        }

        private void InitializeBreakdownFields()
        {
            if (tblBreakdown == null)
            {
                return;
            }

            tblBreakdown.SuspendLayout();
            tblBreakdown.Controls.Clear();
            tblBreakdown.RowStyles.Clear();
            tblBreakdown.RowCount = 0;

            if (_isPayroll)
            {
                InitializePayrollFields();
            }
            else
            {
                InitializeLiquidationFields();
            }

            tblBreakdown.ResumeLayout();
        }

        private void InitializePayrollFields()
        {
            txtContrato = CreateReadOnlyTextBox();
            AddBreakdownRow("Contrato", txtContrato);

            txtCajaCompensacion = CreateReadOnlyTextBox();
            AddBreakdownRow("Caja de compensación", txtCajaCompensacion);

            txtHorasLegales = CreateReadOnlyTextBox();
            AddBreakdownRow("Horas legales", txtHorasLegales);

            numSalarioBase = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Salario base", numSalarioBase);

            numAuxilioTransporte = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Auxilio de transporte", numAuxilioTransporte);

            txtAuxilioDetalle = CreateNoteBox();
            AddBreakdownRow("Detalle auxilio", txtAuxilioDetalle);

            numDevengados = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Devengados", numDevengados);

            numIbc = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("IBC aportes", numIbc);

            numSaludTrabajador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Salud trabajador", numSaludTrabajador);

            numPensionTrabajador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Pensión trabajador", numPensionTrabajador);

            numFsp = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("FSP trabajador", numFsp);

            txtFspDetalle = CreateNoteBox();
            AddBreakdownRow("Detalle FSP", txtFspDetalle);

            numTotalDeducciones = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Total deducciones", numTotalDeducciones);

            numNeto = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Neto a pagar", numNeto);

            numSaludEmpleador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Salud empleador", numSaludEmpleador);

            numPensionEmpleador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Pensión empleador", numPensionEmpleador);

            cmbArlClase = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            cmbArlClase.SelectedIndexChanged += (_, __) =>
            {
                if (_updatingPayroll)
                {
                    return;
                }

                RefreshArlContribution();
                UpdatePayrollTotals();
            };
            AddBreakdownRow("ARL (clase)", cmbArlClase);

            numArlEmpleador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("ARL empleador", numArlEmpleador);

            numCcf = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Caja compensación", numCcf);

            numSena = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("SENA", numSena);

            numIcbf = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("ICBF", numIcbf);

            numTotalEmpleador = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Total aportes empleador", numTotalEmpleador);

            txtEmpleadorDetalle = CreateNoteBox();
            AddBreakdownRow("Detalle empleador", txtEmpleadorDetalle);
        }

        private void InitializeLiquidationFields()
        {
            txtLiqContrato = CreateReadOnlyTextBox();
            AddBreakdownRow("Contrato", txtLiqContrato);

            txtLiqCaja = CreateReadOnlyTextBox();
            AddBreakdownRow("Caja de compensación", txtLiqCaja);

            txtLiqPeriodo = CreateReadOnlyTextBox();
            AddBreakdownRow("Periodo", txtLiqPeriodo);

            txtLiqSalarioDetalle = CreateNoteBox();
            AddBreakdownRow("Detalle salario", txtLiqSalarioDetalle);

            numLiqSalarioPendiente = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Salario pendiente", numLiqSalarioPendiente);

            numLiqCesantias = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Cesantías", numLiqCesantias);

            numLiqIntereses = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Intereses cesantías", numLiqIntereses);

            numLiqPrima = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Prima servicios", numLiqPrima);

            numLiqVacaciones = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Vacaciones", numLiqVacaciones);

            numLiqAuxilio = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Auxilio transporte", numLiqAuxilio);

            txtLiqAuxilioNota = CreateNoteBox();
            AddBreakdownRow("Detalle auxilio", txtLiqAuxilioNota);

            numLiqTotal = CreateCurrencyUpDown(enabled: false);
            AddBreakdownRow("Total a pagar", numLiqTotal);

            txtLiqRecordatorio = CreateNoteBox();
            AddBreakdownRow("Recordatorio", txtLiqRecordatorio);
        }

        private void RenderPayrollSuggestion(PaymentSuggestion suggestion)
        {
            _currentPayroll = suggestion.Payroll;

            if (_currentPayroll == null)
            {
                ClearPayrollFields();
                return;
            }

            var breakdown = _currentPayroll.Value;

            EnsureRiskOptions(breakdown);

            _updatingPayroll = true;

            SetText(txtContrato, breakdown.Contract);
            SetText(txtCajaCompensacion, breakdown.CompensationFund);
            SetText(txtHorasLegales, $"{breakdown.HoursPerMonth} h · Hora: {breakdown.HourlyValue:C2}");

            SetCurrencyValue(numSalarioBase, breakdown.SalaryBase);
            SetCurrencyValue(numAuxilioTransporte, breakdown.TransportAllowance);
            SetNote(txtAuxilioDetalle, breakdown.TransportNote);

            SetCurrencyValue(numDevengados, breakdown.Earnings);
            SetCurrencyValue(numIbc, breakdown.Ibc);
            SetCurrencyValue(numSaludTrabajador, breakdown.EmployeeHealth);
            SetCurrencyValue(numPensionTrabajador, breakdown.EmployeePension);
            SetCurrencyValue(numFsp, breakdown.Fsp);
            SetNote(txtFspDetalle, breakdown.FspNote);
            SetCurrencyValue(numTotalDeducciones, breakdown.EmployeeDeductions);
            SetCurrencyValue(numNeto, breakdown.NetPay);

            SetCurrencyValue(numSaludEmpleador, breakdown.EmployerHealth);
            SetCurrencyValue(numPensionEmpleador, breakdown.EmployerPension);
            SetCurrencyValue(numCcf, breakdown.CompensationFundContribution);
            SetCurrencyValue(numSena, breakdown.Sena);
            SetCurrencyValue(numIcbf, breakdown.Icbf);
            SetNote(txtEmpleadorDetalle, breakdown.EmployerNote);
            SetCurrencyValue(numArlEmpleador, breakdown.EmployerArl);

            _updatingPayroll = false;

            RefreshArlContribution();
            UpdatePayrollTotals();
            PositionButtons();
        }

        private void RenderLiquidationSuggestion(PaymentSuggestion suggestion)
        {
            _currentLiquidation = suggestion.Liquidation;

            if (_currentLiquidation == null)
            {
                ClearLiquidationFields();
                return;
            }

            var breakdown = _currentLiquidation.Value;

            SetText(txtLiqContrato, breakdown.Contract);
            SetText(txtLiqCaja, breakdown.CompensationFund);
            SetText(txtLiqPeriodo, breakdown.PeriodSummary);
            SetNote(txtLiqSalarioDetalle, breakdown.SalaryDetail);

            SetCurrencyValue(numLiqSalarioPendiente, breakdown.SalaryPending);
            SetCurrencyValue(numLiqCesantias, breakdown.Cesantias);
            SetCurrencyValue(numLiqIntereses, breakdown.CesantiasInterest);
            SetCurrencyValue(numLiqPrima, breakdown.PrimaServicios);
            SetCurrencyValue(numLiqVacaciones, breakdown.Vacaciones);
            SetCurrencyValue(numLiqAuxilio, breakdown.TransportComponent);
            SetNote(txtLiqAuxilioNota, breakdown.AuxilioNote);
            SetCurrencyValue(numLiqTotal, breakdown.TotalToPay);
            SetNote(txtLiqRecordatorio, breakdown.Reminder);

            UpdateLiquidationTotals();
            PositionButtons();
        }

        private void PositionButtons()
        {
            if (pnlBreakdown == null || btnCancelar == null || btnGuardar == null)
            {
                return;
            }

            var top = pnlBreakdown.Bottom + 20;
            var maxTop = this.ClientSize.Height - 50;
            if (top > maxTop)
            {
                top = maxTop;
            }
            btnCancelar.Top = top;
            btnGuardar.Top = top;
        }

        private void AdjustBreakdownHeight()
        {
            if (pnlBreakdown == null)
            {
                return;
            }

            var available = this.ClientSize.Height - pnlBreakdown.Top - BreakdownBottomMargin;
            if (available < BreakdownMinHeight)
            {
                available = BreakdownMinHeight;
            }

            pnlBreakdown.Height = available;
            PositionButtons();
        }

        private void AddBreakdownRow(string label, Control control)
        {
            if (tblBreakdown == null)
            {
                return;
            }

            var row = tblBreakdown.RowCount;

            var lbl = new Label
            {
                Text = label.EndsWith(":", StringComparison.Ordinal) ? label : label + ":",
                AutoSize = true,
                Margin = new Padding(3, 6, 3, 6),
                Anchor = AnchorStyles.Left
            };

            control.Margin = new Padding(3, 3, 3, 6);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;

            tblBreakdown.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblBreakdown.Controls.Add(lbl, 0, row);
            tblBreakdown.Controls.Add(control, 1, row);
            tblBreakdown.RowCount = row + 1;
        }

        private TextBox CreateReadOnlyTextBox()
        {
            return new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
        }

        private NumericUpDown CreateCurrencyUpDown(bool enabled)
        {
            return new NumericUpDown
            {
                DecimalPlaces = 2,
                Maximum = 999999999m,
                Minimum = 0m,
                ThousandsSeparator = true,
                Dock = DockStyle.Fill,
                ReadOnly = !enabled,
                TabStop = enabled
            };
        }

        private TextBox CreateNoteBox()
        {
            return new TextBox
            {
                ReadOnly = true,
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor,
                Dock = DockStyle.Fill,
                Height = 56,
                MinimumSize = new System.Drawing.Size(0, 48),
                ScrollBars = ScrollBars.Vertical
            };
        }

        private void SetText(TextBox? textBox, string value)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.Text = value ?? string.Empty;
        }

        private void SetNote(TextBox? textBox, string value)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.Text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private void SetCurrencyValue(NumericUpDown? control, decimal value)
        {
            if (control == null)
            {
                return;
            }

            if (value < control.Minimum)
            {
                value = control.Minimum;
            }
            else if (value > control.Maximum)
            {
                value = control.Maximum;
            }

            control.Value = value;
        }

        private void EnsureRiskOptions(PayrollBreakdown breakdown)
        {
            if (cmbArlClase == null)
            {
                return;
            }

            if (_riskOptions == null || _riskOptions.Length == 0)
            {
                _riskOptions = new[]
                {
                    new RiskOption("Clase I (0,522%)", "Clase I", 0.00522m),
                    new RiskOption("Clase II (1,044%)", "Clase II", 0.01044m),
                    new RiskOption("Clase III (2,436%)", "Clase III", 0.02436m),
                    new RiskOption("Clase IV (4,350%)", "Clase IV", 0.04350m),
                    new RiskOption("Clase V (6,960%)", "Clase V", 0.06960m)
                };
                cmbArlClase.Items.Clear();
                cmbArlClase.Items.AddRange(_riskOptions);
            }

            SelectRiskOption(breakdown);
        }

        private void SelectRiskOption(PayrollBreakdown breakdown)
        {
            if (cmbArlClase == null)
            {
                return;
            }

            RiskOption? match = null;
            if (_riskOptions != null)
            {
                match = _riskOptions.FirstOrDefault(r => Math.Abs(r.Rate - breakdown.ArlRate) < 0.00001m);
            }

            if (match == null)
            {
                match = new RiskOption($"{breakdown.ArlLevel} ({breakdown.ArlRate:P2})", breakdown.ArlLevel, breakdown.ArlRate);
                cmbArlClase.Items.Add(match);
                _riskOptions = cmbArlClase.Items.OfType<RiskOption>().ToArray();
            }

            cmbArlClase.SelectedItem = match;
        }

        private void RefreshArlContribution()
        {
            if (numArlEmpleador == null || numIbc == null || cmbArlClase?.SelectedItem is not RiskOption option)
            {
                return;
            }

            var amount = numIbc.Value * option.Rate;
            SetCurrencyValue(numArlEmpleador, amount);
        }

        private void ClearPayrollFields()
        {
            SetText(txtContrato, string.Empty);
            SetText(txtCajaCompensacion, string.Empty);
            SetText(txtHorasLegales, string.Empty);
            SetNote(txtAuxilioDetalle, string.Empty);
            SetNote(txtFspDetalle, string.Empty);
            SetNote(txtEmpleadorDetalle, string.Empty);

            SetCurrencyValue(numSalarioBase, 0m);
            SetCurrencyValue(numAuxilioTransporte, 0m);
            SetCurrencyValue(numDevengados, 0m);
            SetCurrencyValue(numIbc, 0m);
            SetCurrencyValue(numSaludTrabajador, 0m);
            SetCurrencyValue(numPensionTrabajador, 0m);
            SetCurrencyValue(numFsp, 0m);
            SetCurrencyValue(numTotalDeducciones, 0m);
            SetCurrencyValue(numNeto, 0m);
            SetCurrencyValue(numSaludEmpleador, 0m);
            SetCurrencyValue(numPensionEmpleador, 0m);
            SetCurrencyValue(numArlEmpleador, 0m);
            SetCurrencyValue(numCcf, 0m);
            SetCurrencyValue(numSena, 0m);
            SetCurrencyValue(numIcbf, 0m);
            SetCurrencyValue(numTotalEmpleador, 0m);

            if (cmbArlClase != null)
            {
                cmbArlClase.SelectedIndex = -1;
            }
        }

        private void ClearLiquidationFields()
        {
            SetText(txtLiqContrato, string.Empty);
            SetText(txtLiqCaja, string.Empty);
            SetText(txtLiqPeriodo, string.Empty);
            SetNote(txtLiqSalarioDetalle, string.Empty);
            SetNote(txtLiqAuxilioNota, string.Empty);
            SetNote(txtLiqRecordatorio, string.Empty);

            SetCurrencyValue(numLiqSalarioPendiente, 0m);
            SetCurrencyValue(numLiqCesantias, 0m);
            SetCurrencyValue(numLiqIntereses, 0m);
            SetCurrencyValue(numLiqPrima, 0m);
            SetCurrencyValue(numLiqVacaciones, 0m);
            SetCurrencyValue(numLiqAuxilio, 0m);
            SetCurrencyValue(numLiqTotal, 0m);
        }

        private void UpdatePayrollTotals()
        {
            if (numTotalDeducciones == null || numNeto == null || numTotalEmpleador == null)
            {
                return;
            }

            _updatingPayroll = true;

            decimal totalDeductions = 0m;
            if (numSaludTrabajador != null)
            {
                totalDeductions += numSaludTrabajador.Value;
            }
            if (numPensionTrabajador != null)
            {
                totalDeductions += numPensionTrabajador.Value;
            }
            if (numFsp != null)
            {
                totalDeductions += numFsp.Value;
            }

            SetCurrencyValue(numTotalDeducciones, totalDeductions);

            decimal neto = 0m;
            if (numDevengados != null)
            {
                neto = numDevengados.Value - totalDeductions;
            }
            if (neto < 0)
            {
                neto = 0m;
            }
            SetCurrencyValue(numNeto, neto);

            decimal employerTotal = 0m;
            if (numSaludEmpleador != null)
            {
                employerTotal += numSaludEmpleador.Value;
            }
            if (numPensionEmpleador != null)
            {
                employerTotal += numPensionEmpleador.Value;
            }
            if (numArlEmpleador != null)
            {
                employerTotal += numArlEmpleador.Value;
            }
            if (numCcf != null)
            {
                employerTotal += numCcf.Value;
            }
            if (numSena != null)
            {
                employerTotal += numSena.Value;
            }
            if (numIcbf != null)
            {
                employerTotal += numIcbf.Value;
            }

            SetCurrencyValue(numTotalEmpleador, employerTotal);

            _updatingPayroll = false;
        }

        private void UpdateLiquidationTotals()
        {
            if (numLiqTotal == null)
            {
                return;
            }

            decimal total = 0m;
            if (numLiqSalarioPendiente != null)
            {
                total += numLiqSalarioPendiente.Value;
            }
            if (numLiqCesantias != null)
            {
                total += numLiqCesantias.Value;
            }
            if (numLiqIntereses != null)
            {
                total += numLiqIntereses.Value;
            }
            if (numLiqPrima != null)
            {
                total += numLiqPrima.Value;
            }
            if (numLiqVacaciones != null)
            {
                total += numLiqVacaciones.Value;
            }
            SetCurrencyValue(numLiqTotal, total);
        }

        private sealed class RiskOption
        {
            public RiskOption(string display, string level, decimal rate)
            {
                Display = display;
                Level = level;
                Rate = rate;
            }

            public string Display { get; }
            public string Level { get; }
            public decimal Rate { get; }

            public override string ToString() => Display;
        }
    }

    internal readonly struct SuggestionDetail
    {
        public SuggestionDetail(string label, string value)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Label { get; }
        public string Value { get; }

        public string AsText()
        {
            if (string.IsNullOrWhiteSpace(Label))
            {
                return Value;
            }

            if (string.IsNullOrWhiteSpace(Value))
            {
                return Label;
            }

            return $"{Label}: {Value}";
        }
    }

    internal struct PaymentSuggestion
    {
        public decimal Amount { get; set; }
        public IReadOnlyList<SuggestionDetail> Details { get; set; }
        public PayrollBreakdown? Payroll { get; set; }
        public LiquidationBreakdown? Liquidation { get; set; }
    }

    internal struct PayrollBreakdown
    {
        public string Contract { get; init; }
        public string CompensationFund { get; init; }
        public decimal HoursPerMonth { get; init; }
        public decimal HourlyValue { get; init; }
        public decimal SalaryBase { get; init; }
        public decimal TransportAllowance { get; init; }
        public string TransportNote { get; init; }
        public decimal Earnings { get; init; }
        public decimal Ibc { get; init; }
        public decimal EmployeeHealth { get; init; }
        public decimal EmployeePension { get; init; }
        public decimal Fsp { get; init; }
        public decimal FspRate { get; init; }
        public string FspNote { get; init; }
        public decimal EmployeeDeductions { get; init; }
        public decimal NetPay { get; init; }
        public decimal EmployerHealth { get; init; }
        public decimal EmployerPension { get; init; }
        public decimal EmployerArl { get; init; }
        public decimal CompensationFundContribution { get; init; }
        public decimal Sena { get; init; }
        public decimal Icbf { get; init; }
        public decimal EmployerTotal { get; init; }
        public string EmployerNote { get; init; }
        public string ArlLevel { get; init; }
        public decimal ArlRate { get; init; }
    }

    internal struct LiquidationBreakdown
    {
        public string Contract { get; init; }
        public string CompensationFund { get; init; }
        public string PeriodSummary { get; init; }
        public string SalaryDetail { get; init; }
        public decimal SalaryPending { get; init; }
        public decimal Cesantias { get; init; }
        public decimal CesantiasInterest { get; init; }
        public decimal PrimaServicios { get; init; }
        public decimal Vacaciones { get; init; }
        public decimal TransportComponent { get; init; }
        public string AuxilioNote { get; init; }
        public decimal TotalToPay { get; init; }
        public string Reminder { get; init; }
    }

    internal static class PayrollEngine
    {
        private const decimal HealthRate = 0.04m;
        private const decimal PensionRate = 0.04m;
        private const decimal CesantiasInterestRate = 0.12m;
        private const decimal MinimumSalary2025 = 1423500m;
        private const decimal TransportAllowance2025 = 200000m;
        private const decimal MaximumIbcMultiplier = 25m;
        private const decimal TransportAllowanceThresholdMultiplier = 2m;
        private const decimal EmployerHealthRate = 0.085m;
        private const decimal EmployerPensionRate = 0.12m;
        private const decimal CompensationFundRate = 0.04m;
        private const decimal SenaRate = 0.02m;
        private const decimal IcbfRate = 0.03m;
        private const decimal HoursPerMonth2025 = 220m;
        private const decimal DaysPerMonth = 30m;
        private const decimal DaysPerYear = 360m;
        private const decimal VacationBase = 720m;

        private static readonly RiskProfile[] RiskByPosition = new[]
        {
            new RiskProfile("soldador", "Clase V", 0.06960m),
            new RiskProfile("electricista", "Clase IV", 0.04350m),
            new RiskProfile("mecánico", "Clase IV", 0.04350m),
            new RiskProfile("conductor", "Clase III", 0.02436m),
            new RiskProfile("ingeniero", "Clase III", 0.02436m),
            new RiskProfile("tecnico", "Clase II", 0.01044m),
            new RiskProfile("técnico", "Clase II", 0.01044m),
            new RiskProfile("operario", "Clase II", 0.01044m),
            new RiskProfile("bodega", "Clase II", 0.01044m),
            new RiskProfile("logística", "Clase II", 0.01044m),
            new RiskProfile("logistica", "Clase II", 0.01044m),
            new RiskProfile("supervisor", "Clase II", 0.01044m),
            new RiskProfile("coordinador", "Clase II", 0.01044m),
            new RiskProfile("vendedor", "Clase II", 0.01044m),
            new RiskProfile("cajero", "Clase I", 0.00522m),
            new RiskProfile("auxiliar", "Clase I", 0.00522m),
            new RiskProfile("analista", "Clase I", 0.00522m),
            new RiskProfile("administrador", "Clase I", 0.00522m),
            new RiskProfile("gerente", "Clase I", 0.00522m),
            new RiskProfile("asesor", "Clase I", 0.00522m)
        };

        private static readonly RiskProfile[] RiskByDepartment = new[]
        {
            new RiskProfile("mantenimiento", "Clase IV", 0.04350m),
            new RiskProfile("operaciones", "Clase III", 0.02436m),
            new RiskProfile("producción", "Clase III", 0.02436m),
            new RiskProfile("produccion", "Clase III", 0.02436m),
            new RiskProfile("logística", "Clase II", 0.01044m),
            new RiskProfile("logistica", "Clase II", 0.01044m),
            new RiskProfile("ventas", "Clase II", 0.01044m),
            new RiskProfile("administración", "Clase I", 0.00522m),
            new RiskProfile("administracion", "Clase I", 0.00522m),
            new RiskProfile("sistemas", "Clase I", 0.00522m),
            new RiskProfile("contabilidad", "Clase I", 0.00522m),
            new RiskProfile("rrhh", "Clase I", 0.00522m)
        };

        private static readonly RiskProfile DefaultRisk = new RiskProfile("general", "Clase I", 0.00522m);

        public static PaymentSuggestion BuildPayrollSuggestion(Employee employee)
        {
            var salary = Math.Max(0m, employee?.Salary ?? 0m);
            var ibc = CalculateIbc(salary);
            var transport = CalculateTransportAllowance(salary);
            var devengados = RoundMoney(salary + transport);
            var risk = ResolveRisk(employee);
            var hourlyValue = salary > 0 ? RoundMoney(salary / HoursPerMonth2025) : 0m;

            var salud = RoundMoney(ibc * HealthRate);
            var pension = RoundMoney(ibc * PensionRate);
            var fsp = CalculateFsp(ibc, out var fspRate);
            var totalDeductions = RoundMoney(salud + pension + fsp);
            var neto = RoundMoney(devengados - totalDeductions);
            if (neto < 0)
            {
                neto = 0;
            }

            var employerHealth = RoundMoney(ibc * EmployerHealthRate);
            var employerPension = RoundMoney(ibc * EmployerPensionRate);
            var arlEmployer = RoundMoney(ibc * risk.Rate);
            var compensationFund = RoundMoney(ibc * CompensationFundRate);
            var sena = RoundMoney(ibc * SenaRate);
            var icbf = RoundMoney(ibc * IcbfRate);
            var totalEmployer = RoundMoney(employerHealth + employerPension + arlEmployer + compensationFund + sena + icbf);

            var auxilioText = transport > 0
                ? $"Auxilio de transporte 2025: {transport:C2} (aplica por devengar ≤ 2 SMMLV)."
                : "Sin auxilio de transporte (devenga más de 2 SMMLV o no aplica).";

            var fspText = fspRate > 0
                ? $"FSP {fspRate:P1} ({fsp:C2})"
                : "FSP no aplica (≤4 SMMLV).";

            var details = new List<SuggestionDetail>
            {
                new SuggestionDetail("Contrato", FormatContract(employee)),
                new SuggestionDetail("Caja de compensación", FormatCompensationFund(employee)),
                new SuggestionDetail("Horas legales mes 2025", $"{HoursPerMonth2025} h. Valor hora ordinaria: {hourlyValue:C2}"),
                new SuggestionDetail("Salario base mensual", $"{salary:C2}"),
                new SuggestionDetail("Auxilio de transporte", auxilioText),
                new SuggestionDetail("Devengados estimados", $"{devengados:C2}"),
                new SuggestionDetail("IBC para aportes", $"{ibc:C2} (mín. 1 SMMLV, máx. 25 SMMLV)"),
                new SuggestionDetail("Deducciones trabajador", $"Salud {HealthRate:P0} ({salud:C2}), Pensión {PensionRate:P0} ({pension:C2}), {fspText}"),
                new SuggestionDetail("Total deducciones trabajador", $"{totalDeductions:C2}"),
                new SuggestionDetail("Neto a pagar", $"{neto:C2}"),
                new SuggestionDetail("Aportes empleador", $"Salud {EmployerHealthRate:P1} ({employerHealth:C2}), Pensión {EmployerPensionRate:P0} ({employerPension:C2}), ARL {risk.Level} {risk.Rate:P3} ({arlEmployer:C2}), CCF {CompensationFundRate:P0} ({compensationFund:C2}), SENA {SenaRate:P0} ({sena:C2}), ICBF {IcbfRate:P0} ({icbf:C2})"),
                new SuggestionDetail("Total aportes empleador", $"{totalEmployer:C2}. Verifica exoneración art. 114-1 ET si aplica.")
            };

            var breakdown = new PayrollBreakdown
            {
                Contract = FormatContract(employee),
                CompensationFund = FormatCompensationFund(employee),
                HoursPerMonth = HoursPerMonth2025,
                HourlyValue = hourlyValue,
                SalaryBase = salary,
                TransportAllowance = transport,
                TransportNote = auxilioText,
                Earnings = devengados,
                Ibc = ibc,
                EmployeeHealth = salud,
                EmployeePension = pension,
                Fsp = fsp,
                FspRate = fspRate,
                FspNote = fspText,
                EmployeeDeductions = totalDeductions,
                NetPay = neto,
                EmployerHealth = employerHealth,
                EmployerPension = employerPension,
                EmployerArl = arlEmployer,
                CompensationFundContribution = compensationFund,
                Sena = sena,
                Icbf = icbf,
                EmployerTotal = totalEmployer,
                EmployerNote = $"Salud {EmployerHealthRate:P1} ({employerHealth:C2}), Pensión {EmployerPensionRate:P0} ({employerPension:C2}), ARL {risk.Level} {risk.Rate:P3} ({arlEmployer:C2}), CCF {CompensationFundRate:P0} ({compensationFund:C2}), SENA {SenaRate:P0} ({sena:C2}), ICBF {IcbfRate:P0} ({icbf:C2}). Verifica exoneración art. 114-1 ET si aplica.",
                ArlLevel = risk.Level,
                ArlRate = risk.Rate
            };

            return new PaymentSuggestion
            {
                Amount = neto,
                Details = details,
                Payroll = breakdown
            };
        }

        public static PaymentSuggestion BuildLiquidationSuggestion(Employee employee, DateTime start, DateTime end)
        {
            if (end < start)
            {
                (start, end) = (end, start);
            }

            var salary = Math.Max(0m, employee?.Salary ?? 0m);
            var transport = CalculateTransportAllowance(salary);
            var baseWithAux = salary + transport;
            var daysWorked = Math.Max(1, (end - start).Days + 1);

            var salaryDaily = salary > 0 ? salary / DaysPerMonth : 0m;
            var transportDaily = transport > 0 ? transport / DaysPerMonth : 0m;
            var salaryComponent = RoundMoney(salaryDaily * daysWorked);
            var transportComponent = transport > 0 ? RoundMoney(transportDaily * daysWorked) : 0m;
            var wagesDue = RoundMoney(salaryComponent + transportComponent);

            var cesantias = RoundMoney(baseWithAux * daysWorked / DaysPerYear);
            var interesesCesantias = RoundMoney(cesantias * CesantiasInterestRate * daysWorked / DaysPerYear);
            var prima = RoundMoney(baseWithAux * daysWorked / DaysPerYear);
            var vacaciones = RoundMoney(salary * daysWorked / VacationBase);
            var total = RoundMoney(wagesDue + cesantias + interesesCesantias + prima + vacaciones);

            var wagesLine = transport > 0
                ? $"Salario pendiente por {daysWorked} días: {wagesDue:C2} (salario {salaryComponent:C2} + auxilio {transportComponent:C2})."
                : $"Salario pendiente por {daysWorked} días: {wagesDue:C2}.";

            var auxilioText = transport > 0
                ? "Incluye auxilio de transporte para cesantías, intereses y prima."
                : "Sin auxilio de transporte (devenga >2 SMMLV o no aplica).";

            var periodSummary = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy} ({daysWorked} días)";
            var details = new List<SuggestionDetail>
            {
                new SuggestionDetail("Contrato", FormatContract(employee)),
                new SuggestionDetail("Caja de compensación", FormatCompensationFund(employee)),
                new SuggestionDetail("Periodo liquidado", periodSummary),
                new SuggestionDetail("Salario pendiente", wagesLine),
                new SuggestionDetail("Cesantías", $"{cesantias:C2} (base salarial + auxilio)"),
                new SuggestionDetail("Intereses de cesantías", $"{interesesCesantias:C2} (12% prorrateado)"),
                new SuggestionDetail("Prima de servicios", $"{prima:C2} (base con auxilio)"),
                new SuggestionDetail("Vacaciones", $"{vacaciones:C2} (15 días hábiles por año, sin auxilio)"),
                new SuggestionDetail("Auxilio de transporte", auxilioText),
                new SuggestionDetail("Total a pagar", $"{total:C2} antes de deducciones e indemnizaciones"),
                new SuggestionDetail("Recordatorio", "Recuerda calcular indemnización y deducciones legales (salud, pensión, FSP, retención) si aplican.")
            };

            var breakdown = new LiquidationBreakdown
            {
                Contract = FormatContract(employee),
                CompensationFund = FormatCompensationFund(employee),
                PeriodSummary = periodSummary,
                SalaryDetail = wagesLine,
                SalaryPending = wagesDue,
                Cesantias = cesantias,
                CesantiasInterest = interesesCesantias,
                PrimaServicios = prima,
                Vacaciones = vacaciones,
                TransportComponent = transportComponent,
                AuxilioNote = auxilioText,
                TotalToPay = total,
                Reminder = "Total calculado antes de deducciones e indemnizaciones. Recuerda calcular indemnización y deducciones legales (salud, pensión, FSP, retención) si aplican."
            };

            return new PaymentSuggestion
            {
                Amount = total,
                Details = details,
                Liquidation = breakdown
            };
        }

        private static RiskProfile ResolveRisk(Employee employee)
        {
            var position = (employee?.Position ?? string.Empty).ToLowerInvariant();
            foreach (var profile in RiskByPosition)
            {
                if (position.Contains(profile.Keyword))
                {
                    return profile;
                }
            }

            var department = (employee?.Department ?? string.Empty).ToLowerInvariant();
            foreach (var profile in RiskByDepartment)
            {
                if (department.Contains(profile.Keyword))
                {
                    return profile;
                }
            }

            return DefaultRisk;
        }

        private static decimal CalculateIbc(decimal salary)
        {
            if (salary <= 0)
            {
                return 0m;
            }

            var min = MinimumSalary2025;
            var max = MinimumSalary2025 * MaximumIbcMultiplier;
            var ibc = salary;
            if (ibc < min)
            {
                ibc = min;
            }

            if (ibc > max)
            {
                ibc = max;
            }

            return ibc;
        }

        private static decimal CalculateTransportAllowance(decimal salary)
        {
            return IsTransportAllowanceEligible(salary) ? TransportAllowance2025 : 0m;
        }

        private static bool IsTransportAllowanceEligible(decimal salary)
        {
            if (salary <= 0)
            {
                return false;
            }

            var threshold = MinimumSalary2025 * TransportAllowanceThresholdMultiplier;
            return salary <= threshold;
        }

        private static decimal CalculateFsp(decimal ibc, out decimal rate)
        {
            rate = GetFspRate(ibc);
            if (rate <= 0 || ibc <= 0)
            {
                return 0m;
            }

            return RoundMoney(ibc * rate);
        }

        private static decimal GetFspRate(decimal ibc)
        {
            if (ibc <= 0)
            {
                return 0m;
            }

            var smmlv = MinimumSalary2025;
            if (ibc <= smmlv * 4m)
            {
                return 0m;
            }

            if (ibc <= smmlv * 16m)
            {
                return 0.01m;
            }

            if (ibc <= smmlv * 17m)
            {
                return 0.012m;
            }

            if (ibc <= smmlv * 18m)
            {
                return 0.014m;
            }

            if (ibc <= smmlv * 19m)
            {
                return 0.016m;
            }

            if (ibc <= smmlv * 20m)
            {
                return 0.018m;
            }

            return 0.02m;
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string FormatContract(Employee employee)
        {
            var type = employee?.ContractType;
            var duration = employee?.ContractDuration;
            if (string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(duration))
            {
                return "Sin información";
            }

            if (string.IsNullOrWhiteSpace(duration))
            {
                return type?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                return duration?.Trim() ?? string.Empty;
            }

            return $"{type.Trim()} · {duration.Trim()}";
        }

        private static string FormatCompensationFund(Employee employee)
        {
            var fund = employee?.CompensationFund;
            return string.IsNullOrWhiteSpace(fund) ? "Sin caja registrada" : fund.Trim();
        }

        private struct RiskProfile
        {
            public RiskProfile(string keyword, string level, decimal rate)
            {
                Keyword = keyword;
                Level = level;
                Rate = rate;
            }

            public string Keyword { get; }
            public string Level { get; }
            public decimal Rate { get; }
        }
    }
}
