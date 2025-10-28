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
        e.Dependents,
        e.HealthProvider,
        e.PensionProvider,
        e.BloodType,
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
                    Dependents = rd.IsDBNull(17) ? 0 : rd.GetInt32(17),
                    HealthProvider = rd.IsDBNull(18) ? "" : rd.GetString(18),
                    PensionProvider = rd.IsDBNull(19) ? "" : rd.GetString(19),
                    BloodType = rd.IsDBNull(20) ? "" : rd.GetString(20),
                    LastPayrollPeriodStart = rd.IsDBNull(21) ? (DateTime?)null : rd.GetDateTime(21),
                    LastPayrollPeriodEnd = rd.IsDBNull(22) ? (DateTime?)null : rd.GetDateTime(22),
                    LastPayrollAmount = rd.IsDBNull(23) ? (decimal?)null : rd.GetDecimal(23),
                    LastPayrollNotes = rd.IsDBNull(24) ? "" : rd.GetString(24),
                    LastLiquidationPeriodStart = rd.IsDBNull(25) ? (DateTime?)null : rd.GetDateTime(25),
                    LastLiquidationPeriodEnd = rd.IsDBNull(26) ? (DateTime?)null : rd.GetDateTime(26),
                    LastLiquidationAmount = rd.IsDBNull(27) ? (decimal?)null : rd.GetDecimal(27),
                    LastLiquidationNotes = rd.IsDBNull(28) ? "" : rd.GetString(28)
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
                    (e.BirthDate.HasValue && e.BirthDate.Value.ToString("dd/MM/yyyy").ToLower().Contains(q)) ||
                    e.Dependents.ToString().Contains(rawQuery));
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
INSERT INTO dbo.Employees(Name,Email,Phone,Department,Position,Status,Salary,HireDate,DocumentNumber,Address,BankAccount,EmergencyContact,EmergencyPhone,BirthDate,Gender,MaritalStatus,Dependents,HealthProvider,PensionProvider,BloodType)
OUTPUT INSERTED.Id
VALUES(@n,@e,@p,@d,@po,@s,@sa,@h,@doc,@addr,@bank,@emc,@emp,@birth,@gender,@marital,@deps,@health,@pension,@blood);", cn);
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
                    cmd.Parameters.AddWithValue("@deps", dlg.Result.Dependents);
                    cmd.Parameters.AddWithValue("@health", string.IsNullOrWhiteSpace(dlg.Result.HealthProvider) ? (object)DBNull.Value : dlg.Result.HealthProvider);
                    cmd.Parameters.AddWithValue("@pension", string.IsNullOrWhiteSpace(dlg.Result.PensionProvider) ? (object)DBNull.Value : dlg.Result.PensionProvider);
                    cmd.Parameters.AddWithValue("@blood", string.IsNullOrWhiteSpace(dlg.Result.BloodType) ? (object)DBNull.Value : dlg.Result.BloodType);
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
 BirthDate=@birth, Gender=@gender, MaritalStatus=@marital, Dependents=@deps, HealthProvider=@health,
 PensionProvider=@pension, BloodType=@blood
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
                    cmd.Parameters.AddWithValue("@deps", dlg.Result.Dependents);
                    cmd.Parameters.AddWithValue("@health", string.IsNullOrWhiteSpace(dlg.Result.HealthProvider) ? (object)DBNull.Value : dlg.Result.HealthProvider);
                    cmd.Parameters.AddWithValue("@pension", string.IsNullOrWhiteSpace(dlg.Result.PensionProvider) ? (object)DBNull.Value : dlg.Result.PensionProvider);
                    cmd.Parameters.AddWithValue("@blood", string.IsNullOrWhiteSpace(dlg.Result.BloodType) ? (object)DBNull.Value : dlg.Result.BloodType);
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
                lblDDependientes.Text = "-";
                lblDSalud.Text = "-";
                lblDPension.Text = "-";
                lblDTipoSangre.Text = "-";
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
            lblDDependientes.Text = e.Dependents.ToString();
            lblDSalud.Text = string.IsNullOrWhiteSpace(e.HealthProvider) ? "-" : e.HealthProvider;
            lblDPension.Text = string.IsNullOrWhiteSpace(e.PensionProvider) ? "-" : e.PensionProvider;
            lblDTipoSangre.Text = string.IsNullOrWhiteSpace(e.BloodType) ? "-" : e.BloodType;
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
        public int Dependents { get; set; }
        public string HealthProvider { get; set; } = "";
        public string PensionProvider { get; set; } = "";
        public string BloodType { get; set; } = "";
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

        private TextBox txtName;
        private TextBox txtDocument;
        private DateTimePicker dtpBirthDate;
        private CheckBox chkNoBirthDate;
        private ComboBox cmbGender;
        private ComboBox cmbMaritalStatus;
        private NumericUpDown numDependents;
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
                int dependents = existing.Dependents;
                if (dependents < numDependents.Minimum)
                {
                    numDependents.Value = numDependents.Minimum;
                }
                else if (dependents > numDependents.Maximum)
                {
                    numDependents.Value = numDependents.Maximum;
                }
                else
                {
                    numDependents.Value = dependents;
                }
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
            this.Controls.Add(dtpBirthDate);
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
            this.Controls.Add(chkNoBirthDate);
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

            // Personas a cargo
            AddLabel("Personas a cargo:", 20, y);
            numDependents = new NumericUpDown
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Minimum = 0,
                Maximum = 50,
                DecimalPlaces = 0
            };
            this.Controls.Add(numDependents);
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
            this.Controls.Add(numSalary);
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
            Result.DocumentNumber = txtDocument.Text.Trim();
            Result.BirthDate = chkNoBirthDate.Checked ? (DateTime?)null : dtpBirthDate.Value.Date;
            Result.Gender = cmbGender.Text.Trim();
            Result.MaritalStatus = cmbMaritalStatus.Text.Trim();
            Result.Dependents = (int)numDependents.Value;
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
        private TextBox txtNotes;
        private Button btnGuardar;
        private Button btnCancelar;

        public EmployeePaymentDialog(Employee employee, string friendlyName, bool isPayroll)
        {
            _employee = employee;
            _isPayroll = isPayroll;

            this.Text = "Registrar " + friendlyName;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Size = new System.Drawing.Size(420, 420);

            BuildForm();
        }

        private void BuildForm()
        {
            int y = 20;
            int lblWidth = 130;
            int ctrlX = lblWidth + 30;
            int ctrlWidth = 220;

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
            this.Controls.Add(numAmount);
            y += 35;

            AddLabel("Notas:", 20, y);
            txtNotes = new TextBox
            {
                Location = new System.Drawing.Point(ctrlX, y),
                Width = ctrlWidth,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(txtNotes);
            y += 100;

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

            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            var today = DateTime.Today;
            DateTime startDefault;
            DateTime endDefault;

            if (_isPayroll)
            {
                startDefault = new DateTime(today.Year, today.Month, 1);
                endDefault = startDefault.AddMonths(1).AddDays(-1);
                numAmount.Value = _employee.Salary > 0 ? Math.Min(numAmount.Maximum, _employee.Salary) : 0;
                chkNoStart.Checked = false;
            }
            else
            {
                startDefault = _employee.HireDate ?? today;
                endDefault = today;
                chkNoStart.Checked = !_employee.HireDate.HasValue;
                numAmount.Value = 0;
            }

            var startValue = startDefault < dtpStart.MinDate ? dtpStart.MinDate : startDefault;
            var endValue = endDefault < dtpEnd.MinDate ? dtpEnd.MinDate : endDefault;

            if (endValue < startValue && !chkNoStart.Checked)
            {
                endValue = startValue;
            }

            dtpStart.Value = startValue;
            dtpEnd.Value = endValue;

            if (chkNoStart.Checked)
            {
                dtpStart.Enabled = false;
            }
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
                Notes = txtNotes.Text.Trim()
            };
        }
    }
}
