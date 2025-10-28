using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Empleados
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblTitulo;
        private TextBox txtBuscar;
        private ComboBox cmbDept;
        private ComboBox cmbEstado;
        private Button btnNuevo;
        private Button btnEditar;
        private Button btnToggleDetalle;
        private Button btnEliminar;
        private Button btnNomina;
        private Button btnLiquidacion;

        private SplitContainer contentSplit;
        private DataGridView grid;
        private Panel actionBar;
        private FlowLayoutPanel actionButtons;
        private Label lblStatus;
        private Panel pnlDetalle;
        private Label lblDNombre;
        private Label lblDCargo;
        private Label lblDEstado;
        private Label lblDEmail;
        private Label lblDPhone;
        private Label lblDDept;
        private Label lblDIngreso;
        private Label lblDSalario;
        private Label lblDDocumento;
        private Label lblDDireccion;
        private Label lblDBanco;
        private Label lblDNacimiento;
        private Label lblDGenero;
        private Label lblDEstadoCivil;
        private Label lblDDependientes;
        private Label lblDSalud;
        private Label lblDPension;
        private Label lblDTipoSangre;
        private Label lblDContactoEmergencia;
        private Label lblDTelefonoEmergencia;
        private Label lblDUltimaNomina;
        private Label lblDUltimaLiquidacion;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = ModernTheme.Background;
            ClientSize = new Size(1400, 860);
            Font = ModernTheme.Body;
            ForeColor = ModernTheme.TextPrimary;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Gestión de Empleados";

            layoutRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24),
                BackColor = Color.Transparent
            };
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // topBar
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // actionBar
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // content

            BuildTopBar();
            BuildActionBar();
            BuildContent();

            layoutRoot.Controls.Add(topBar, 0, 0);
            layoutRoot.Controls.Add(actionBar, 0, 1);
            layoutRoot.Controls.Add(contentSplit, 0, 2);

            Controls.Add(layoutRoot);
        }

        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 16)
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblTitulo = new Label
            {
                Text = "Empleados",
                Font = ModernTheme.Heading2,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var controlBar = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            txtBuscar = new TextBox
            {
                PlaceholderText = "Buscar nombre, contacto o cualquier dato…",
                Width = 320,
                Margin = new Padding(0, 0, 12, 12)
            };

            cmbDept = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Margin = new Padding(0, 0, 12, 12)
            };

            cmbEstado = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Margin = new Padding(0, 0, 24, 12)
            };

            btnToggleDetalle = ModernTheme.CreateGhostButton("◀ Ocultar detalle");
            btnToggleDetalle.Margin = new Padding(0, 0, 0, 12);
            btnToggleDetalle.Width = 160;

            controlBar.Controls.Add(txtBuscar);
            controlBar.Controls.Add(cmbDept);
            controlBar.Controls.Add(cmbEstado);
            controlBar.Controls.Add(btnToggleDetalle);

            headerLayout.Controls.Add(lblTitulo, 0, 0);
            headerLayout.Controls.Add(controlBar, 0, 1);

            topBar.Controls.Add(headerLayout);
        }

        private void BuildActionBar()
        {
            actionBar = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, 16)
            };

            var actionLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblStatus = new Label
            {
                Text = "Empleados: 0",
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(0, 6, 0, 0)
            };

            actionButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            btnNuevo = ModernTheme.CreatePrimaryButton("＋ Nuevo empleado");
            btnNuevo.Margin = new Padding(8, 0, 0, 0);

            btnEditar = ModernTheme.CreateGhostButton("Editar seleccionado");
            btnEditar.Margin = new Padding(8, 0, 0, 0);

            btnEliminar = ModernTheme.CreateGhostButton("Eliminar seleccionado");
            btnEliminar.Margin = new Padding(8, 0, 0, 0);
            btnEliminar.Width = 170;

            btnNomina = ModernTheme.CreateGhostButton("Registrar nómina");
            btnNomina.Margin = new Padding(8, 0, 0, 0);
            btnNomina.Width = 160;

            btnLiquidacion = ModernTheme.CreateGhostButton("Registrar liquidación");
            btnLiquidacion.Margin = new Padding(8, 0, 0, 0);
            btnLiquidacion.Width = 190;

            actionButtons.Controls.Add(btnLiquidacion);
            actionButtons.Controls.Add(btnNomina);
            actionButtons.Controls.Add(btnEliminar);
            actionButtons.Controls.Add(btnEditar);
            actionButtons.Controls.Add(btnNuevo);

            actionLayout.Controls.Add(lblStatus, 0, 0);
            actionLayout.Controls.Add(actionButtons, 1, 0);
            actionLayout.SetColumnSpan(actionButtons, 1);

            actionBar.Controls.Add(actionLayout);
        }

        private void BuildContent()
        {
            contentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 850,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                SplitterWidth = 8
            };

            var gridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(16)
            };
            gridCard.Paint += (s, e) => DrawCard(e.Graphics, gridCard.ClientRectangle);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None
            };
            ModernTheme.StyleDataGrid(grid);

            gridCard.Controls.Add(grid);
            contentSplit.Panel1.Controls.Add(gridCard);

            pnlDetalle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Surface,
                Padding = new Padding(24)
            };
            pnlDetalle.Paint += (s, e) => DrawCard(e.Graphics, pnlDetalle.ClientRectangle);

            var lblHeader = new Label
            {
                Text = "Detalle del empleado",
                Font = ModernTheme.Heading3,
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 32
            };

            lblDNombre = new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 36
            };

            lblDCargo = CreateDetailLabel();
            lblDEstado = CreateDetailLabel();
            lblDEmail = CreateDetailLabel();
            lblDPhone = CreateDetailLabel();
            lblDDept = CreateDetailLabel();
            lblDIngreso = CreateDetailLabel();
            lblDSalario = CreateDetailLabel();
            lblDDocumento = CreateDetailLabel();
            lblDDireccion = CreateDetailLabel();
            lblDBanco = CreateDetailLabel();
            lblDNacimiento = CreateDetailLabel();
            lblDGenero = CreateDetailLabel();
            lblDEstadoCivil = CreateDetailLabel();
            lblDDependientes = CreateDetailLabel();
            lblDSalud = CreateDetailLabel();
            lblDPension = CreateDetailLabel();
            lblDTipoSangre = CreateDetailLabel();
            lblDContactoEmergencia = CreateDetailLabel();
            lblDTelefonoEmergencia = CreateDetailLabel();
            lblDUltimaNomina = CreateDetailLabel();
            lblDUltimaLiquidacion = CreateDetailLabel();

            var detailList = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };
            detailList.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            detailList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            AddDetail(detailList, "Cargo", lblDCargo);
            AddDetail(detailList, "Estado", lblDEstado);
            AddDetail(detailList, "Email", lblDEmail);
            AddDetail(detailList, "Teléfono", lblDPhone);
            AddDetail(detailList, "Documento", lblDDocumento);
            AddDetail(detailList, "Departamento", lblDDept);
            AddDetail(detailList, "Dirección", lblDDireccion);
            AddDetail(detailList, "Ingreso", lblDIngreso);
            AddDetail(detailList, "Salario", lblDSalario);
            AddDetail(detailList, "Fecha nacimiento", lblDNacimiento);
            AddDetail(detailList, "Género", lblDGenero);
            AddDetail(detailList, "Estado civil", lblDEstadoCivil);
            AddDetail(detailList, "Personas a cargo", lblDDependientes);
            AddDetail(detailList, "Salud/EPS", lblDSalud);
            AddDetail(detailList, "Pensión/AFP", lblDPension);
            AddDetail(detailList, "Tipo de sangre", lblDTipoSangre);
            AddDetail(detailList, "Cuenta bancaria", lblDBanco);
            AddDetail(detailList, "Contacto emergencia", lblDContactoEmergencia);
            AddDetail(detailList, "Teléfono emergencia", lblDTelefonoEmergencia);
            AddDetail(detailList, "Última nómina", lblDUltimaNomina);
            AddDetail(detailList, "Última liquidación", lblDUltimaLiquidacion);

            pnlDetalle.Controls.Add(detailList);
            pnlDetalle.Controls.Add(lblDNombre);
            pnlDetalle.Controls.Add(lblHeader);

            contentSplit.Panel2.Controls.Add(pnlDetalle);
        }

        private static void AddDetail(TableLayoutPanel panel, string caption, Control value)
        {
            var lbl = new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(0, 6, 16, 6),
                AutoSize = true
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(value);
        }

        private static Label CreateDetailLabel()
        {
            return new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Margin = new Padding(0, 6, 0, 6),
                AutoSize = true
            };
        }

        private static void DrawCard(Graphics g, Rectangle bounds)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 18;
            int diameter = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            using var brush = new SolidBrush(ModernTheme.Surface);
            using var border = new Pen(ModernTheme.Border);
            g.FillPath(brush, path);
            g.DrawPath(border, path);
        }
    }
}