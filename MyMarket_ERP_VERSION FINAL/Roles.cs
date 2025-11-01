using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public partial class Roles : Form
    {
        private static readonly (NavSection Section, string Label)[] ModuleOptions = new[]
        {
            (NavSection.Central, "Dashboard"),
            (NavSection.Compras, "Compras"),
            (NavSection.Clientes, "Clientes"),
            (NavSection.Inventario, "Inventario"),
            (NavSection.Contabilidad, "Contabilidad"),
            (NavSection.Empleados, "Empleados"),
            (NavSection.Roles, "Gestión de roles"),
            (NavSection.Historial, "Historial de compras")
        };

        private readonly Dictionary<NavSection, CheckBox> _moduleChecks = new();
        private RoleDefinition? _current;
        private bool _isNewRole;
        private bool _suppressSelection;

        public Roles()
        {
            InitializeComponent();

            var role = AppSession.Role;
            Tag = NavSection.Roles;

            SidebarInstaller.Install(
                this,
                role,
                NavSection.Roles,
                section => NavigationService.Open(section, this, role)
            );

            SetupGrid();
            BuildModuleCheckboxes();

            btnNuevo.Click += (_, __) => StartNewRole();
            btnRefrescar.Click += (_, __) => ReloadRoles();
            btnGuardar.Click += (_, __) => SaveCurrentRole();
            gridRoles.SelectionChanged += GridRoles_SelectionChanged;

            Shown += (_, __) => ReloadRoles();
        }

        private void SetupGrid()
        {
            gridRoles.AutoGenerateColumns = false;
            gridRoles.ReadOnly = true;
            gridRoles.AllowUserToAddRows = false;
            gridRoles.AllowUserToDeleteRows = false;
            gridRoles.MultiSelect = false;
            gridRoles.Columns.Clear();

            ModernTheme.StyleDataGrid(gridRoles);
            gridRoles.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Rol",
                Name = "colRole",
                Width = 140
            });
            gridRoles.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Estado",
                Name = "colStatus",
                Width = 120
            });
            gridRoles.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Módulos",
                Name = "colModules",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void BuildModuleCheckboxes()
        {
            modulesPanel.SuspendLayout();
            modulesPanel.Controls.Clear();
            _moduleChecks.Clear();

            foreach (var (section, label) in ModuleOptions)
            {
                var check = new CheckBox
                {
                    Text = label,
                    Tag = section,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 16, 8)
                };
                modulesPanel.Controls.Add(check);
                _moduleChecks[section] = check;
            }

            modulesPanel.ResumeLayout();
        }

        private void ReloadRoles(int? focusId = null)
        {
            try
            {
                var roles = RolesRepository.GetAll();
                PopulateGrid(roles);

                if (roles.Count == 0)
                {
                    ClearDetails();
                    return;
                }

                focusId ??= _current?.Id;
                var targetRow = FindRowById(focusId) ?? (gridRoles.Rows.Count > 0 ? gridRoles.Rows[0] : null);

                if (targetRow != null)
                {
                    SelectRow(targetRow);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Error al cargar los roles:\n" + ex.Message, "SQL Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los roles:\n" + ex.Message, "Roles",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateGrid(IReadOnlyCollection<RoleDefinition> roles)
        {
            _suppressSelection = true;
            gridRoles.Rows.Clear();

            foreach (var role in roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                var summary = role.Modules.Count == 0
                    ? "Sin módulos"
                    : string.Join(", ", role.Modules
                        .OrderBy(m => m.ToString(), StringComparer.OrdinalIgnoreCase)
                        .Select(FormatModuleName));

                var rowIndex = gridRoles.Rows.Add(role.Name,
                    role.IsActive ? "Activo" : "Pendiente",
                    summary);

                gridRoles.Rows[rowIndex].Tag = role;
            }

            _suppressSelection = false;
        }

        private void GridRoles_SelectionChanged(object? sender, EventArgs e)
        {
            if (_suppressSelection)
                return;

            if (gridRoles.CurrentRow?.Tag is RoleDefinition role)
            {
                DisplayRole(role, isNew: false);
            }
        }

        private void SelectRow(DataGridViewRow row)
        {
            _suppressSelection = true;
            gridRoles.ClearSelection();
            row.Selected = true;
            gridRoles.CurrentCell = row.Cells[0];
            _suppressSelection = false;
            DisplayRole((RoleDefinition)row.Tag, isNew: false);
        }

        private DataGridViewRow? FindRowById(int? roleId)
        {
            if (roleId == null)
                return null;

            foreach (DataGridViewRow row in gridRoles.Rows)
            {
                if (row.Tag is RoleDefinition role && role.Id == roleId.Value)
                {
                    return row;
                }
            }

            return null;
        }

        private void DisplayRole(RoleDefinition role, bool isNew)
        {
            _current = CloneRole(role);
            _isNewRole = isNew;

            txtNombre.Text = _current.Name;
            txtDescripcion.Text = _current.Description ?? string.Empty;

            var canRename = isNew || !_current.IsSystem;
            txtNombre.ReadOnly = !canRename;
            txtNombre.BackColor = canRename ? Color.White : Color.FromArgb(248, 250, 252);

            chkActivo.Enabled = !_current.IsSystem || isNew;
            chkActivo.Checked = _current.IsSystem || _current.IsActive;

            lblEstado.Text = isNew
                ? "Estado: nuevo rol"
                : _current.IsActive ? "Estado: activo" : "Estado: pendiente";
            lblEstado.ForeColor = isNew
                ? ModernTheme.Accent
                : _current.IsActive ? ModernTheme.AccentSuccess : ModernTheme.AccentWarning;

            foreach (var check in _moduleChecks.Values)
            {
                check.Enabled = true;
                check.Checked = false;
            }

            foreach (var module in _current.Modules)
            {
                if (_moduleChecks.TryGetValue(module, out var check))
                {
                    check.Checked = true;
                }
            }
        }

        private void StartNewRole()
        {
            gridRoles.ClearSelection();
            _current = new RoleDefinition
            {
                Name = string.Empty,
                Description = string.Empty,
                IsActive = false,
                IsSystem = false
            };
            _isNewRole = true;
            foreach (var check in _moduleChecks.Values)
            {
                check.Enabled = true;
            }
            DisplayRole(_current, isNew: true);
        }

        private void SaveCurrentRole()
        {
            if (_current == null)
            {
                MessageBox.Show("Selecciona o crea un rol antes de guardar.", "Roles",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var name = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("El nombre del rol es obligatorio.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNombre.Focus();
                return;
            }

            var selectedModules = _moduleChecks
                .Where(kvp => kvp.Value.Checked)
                .Select(kvp => kvp.Key)
                .ToList();

            if (selectedModules.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un módulo para el rol.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isNewRole || !_current.IsSystem)
            {
                _current.Name = name;
            }

            _current.Description = string.IsNullOrWhiteSpace(txtDescripcion.Text)
                ? null
                : txtDescripcion.Text.Trim();

            _current.Modules.Clear();
            foreach (var module in selectedModules)
            {
                _current.Modules.Add(module);
            }

            if (_current.IsSystem && !_isNewRole)
            {
                _current.IsActive = true;
            }
            else
            {
                _current.IsActive = chkActivo.Checked;
            }

            try
            {
                var allowRename = _isNewRole || !_current.IsSystem;
                RolesRepository.Save(_current, allowRename);
                Permissions.Reload();

                MessageBox.Show("Los cambios se guardaron correctamente.", "Roles",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                var focusId = _current.Id;
                _isNewRole = false;
                ReloadRoles(focusId);
            }
            catch (SqlException ex)
            {
                MessageBox.Show("No fue posible guardar el rol:\n" + ex.Message, "SQL Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error al guardar el rol:\n" + ex.Message, "Roles",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearDetails()
        {
            _current = null;
            _isNewRole = false;
            txtNombre.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtNombre.ReadOnly = true;
            txtNombre.BackColor = Color.FromArgb(248, 250, 252);
            chkActivo.Checked = false;
            chkActivo.Enabled = false;
            lblEstado.Text = "Estado: (sin selección)";
            lblEstado.ForeColor = ModernTheme.TextSecondary;
            foreach (var check in _moduleChecks.Values)
            {
                check.Checked = false;
                check.Enabled = false;
            }
        }

        private static RoleDefinition CloneRole(RoleDefinition source)
        {
            var clone = new RoleDefinition
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                IsActive = source.IsActive,
                IsSystem = source.IsSystem
            };

            foreach (var module in source.Modules)
            {
                clone.Modules.Add(module);
            }

            return clone;
        }

        private static string FormatModuleName(NavSection section) => section switch
        {
            NavSection.Central => "Dashboard",
            NavSection.Compras => "Compras",
            NavSection.Clientes => "Clientes",
            NavSection.Historial => "Historial",
            NavSection.Inventario => "Inventario",
            NavSection.Contabilidad => "Contabilidad",
            NavSection.Empleados => "Empleados",
            NavSection.Roles => "Roles",
            _ => section.ToString()
        };
    }
}
