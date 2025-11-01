using System.Windows.Forms;

namespace MyMarket_ERP
{
    partial class Roles
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer? components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            layoutRoot = new TableLayoutPanel();
            topBar = new Panel();
            lblDescripcion = new Label();
            lblTitulo = new Label();
            contentLayout = new TableLayoutPanel();
            rolesCard = new Panel();
            gridRoles = new DataGridView();
            rolesActions = new FlowLayoutPanel();
            btnNuevo = new Button();
            btnRefrescar = new Button();
            lblLista = new Label();
            detailCard = new Panel();
            btnToggleDetalles = new Button();
            detailLayout = new TableLayoutPanel();
            lblEstado = new Label();
            lblNombre = new Label();
            txtNombre = new TextBox();
            lblDetalleDescripcion = new Label();
            txtDescripcion = new TextBox();
            chkActivo = new CheckBox();
            lblCorreosTitulo = new Label();
            emailsPanel = new FlowLayoutPanel();
            lblModulos = new Label();
            modulesPanel = new FlowLayoutPanel();
            lblAyuda = new Label();
            detailActions = new FlowLayoutPanel();
            btnGuardar = new Button();
            lblDetalleTitulo = new Label();
            layoutRoot.SuspendLayout();
            topBar.SuspendLayout();
            contentLayout.SuspendLayout();
            rolesCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridRoles).BeginInit();
            rolesActions.SuspendLayout();
            detailCard.SuspendLayout();
            detailLayout.SuspendLayout();
            detailActions.SuspendLayout();
            SuspendLayout();
            // 
            // layoutRoot
            // 
            layoutRoot.ColumnCount = 1;
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutRoot.Controls.Add(topBar, 0, 0);
            layoutRoot.Controls.Add(contentLayout, 0, 1);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.Location = new System.Drawing.Point(0, 0);
            layoutRoot.Name = "layoutRoot";
            layoutRoot.RowCount = 2;
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutRoot.Size = new System.Drawing.Size(1180, 720);
            layoutRoot.TabIndex = 0;
            // 
            // topBar
            // 
            topBar.BackColor = System.Drawing.Color.White;
            topBar.Controls.Add(lblDescripcion);
            topBar.Controls.Add(lblTitulo);
            topBar.Dock = DockStyle.Fill;
            topBar.Location = new System.Drawing.Point(3, 3);
            topBar.Name = "topBar";
            topBar.Padding = new Padding(24);
            topBar.Size = new System.Drawing.Size(1174, 96);
            topBar.TabIndex = 0;
            // 
            // lblDescripcion
            // 
            lblDescripcion.AutoSize = true;
            lblDescripcion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblDescripcion.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            lblDescripcion.Location = new System.Drawing.Point(27, 54);
            lblDescripcion.Name = "lblDescripcion";
            lblDescripcion.Size = new System.Drawing.Size(393, 23);
            lblDescripcion.TabIndex = 1;
            lblDescripcion.Text = "Administra qué módulos puede usar cada tipo de rol.";
            // 
            // lblTitulo
            //
            lblTitulo.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            lblTitulo.AutoSize = false;
            lblTitulo.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblTitulo.ForeColor = new System.Drawing.Color.FromArgb(15, 23, 42);
            lblTitulo.Location = new System.Drawing.Point(24, 16);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new System.Drawing.Size(600, 50);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Gestión de roles";
            lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            contentLayout.ColumnCount = 2;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.Controls.Add(rolesCard, 0, 0);
            contentLayout.Controls.Add(detailCard, 1, 0);
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.Location = new System.Drawing.Point(3, 105);
            contentLayout.Name = "contentLayout";
            contentLayout.RowCount = 1;
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.Size = new System.Drawing.Size(1174, 612);
            contentLayout.TabIndex = 1;
            // 
            // rolesCard
            // 
            rolesCard.BackColor = System.Drawing.Color.White;
            rolesCard.Controls.Add(gridRoles);
            rolesCard.Controls.Add(rolesActions);
            rolesCard.Controls.Add(lblLista);
            rolesCard.Dock = DockStyle.Fill;
            rolesCard.Location = new System.Drawing.Point(3, 3);
            rolesCard.Name = "rolesCard";
            rolesCard.Padding = new Padding(24);
            rolesCard.Size = new System.Drawing.Size(354, 606);
            rolesCard.TabIndex = 0;
            // 
            // gridRoles
            // 
            gridRoles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridRoles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridRoles.Location = new System.Drawing.Point(27, 128);
            gridRoles.MultiSelect = false;
            gridRoles.Name = "gridRoles";
            gridRoles.RowHeadersVisible = false;
            gridRoles.RowTemplate.Height = 40;
            gridRoles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridRoles.Size = new System.Drawing.Size(300, 451);
            gridRoles.TabIndex = 2;
            // 
            // rolesActions
            // 
            rolesActions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rolesActions.Controls.Add(btnNuevo);
            rolesActions.Controls.Add(btnToggleDetalles);
            rolesActions.FlowDirection = FlowDirection.LeftToRight;
            rolesActions.Location = new System.Drawing.Point(24, 72);
            rolesActions.Name = "rolesActions";
            rolesActions.Size = new System.Drawing.Size(306, 50);
            rolesActions.TabIndex = 1;
            rolesActions.WrapContents = false;
            // 
            // btnNuevo
            // 
            btnNuevo.AutoSize = true;
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.FlatStyle = FlatStyle.Flat;
            btnNuevo.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            btnNuevo.ForeColor = System.Drawing.Color.White;
            btnNuevo.BackColor = System.Drawing.Color.FromArgb(99, 102, 241);
            btnNuevo.Location = new System.Drawing.Point(3, 3);
            btnNuevo.Name = "btnNuevo";
            btnNuevo.Padding = new Padding(16, 8, 16, 8);
            btnNuevo.Size = new System.Drawing.Size(129, 42);
            btnNuevo.TabIndex = 0;
            btnNuevo.Text = "Nuevo rol";
            btnNuevo.UseVisualStyleBackColor = false;
            // 
            // btnRefrescar
            // 
            btnRefrescar.AutoSize = true;
            btnRefrescar.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(226, 232, 240);
            btnRefrescar.FlatStyle = FlatStyle.Flat;
            btnRefrescar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnRefrescar.ForeColor = System.Drawing.Color.FromArgb(79, 70, 229);
            btnRefrescar.Location = new System.Drawing.Point(116, 3);
            btnRefrescar.Name = "btnRefrescar";
            btnRefrescar.Padding = new Padding(16, 8, 16, 8);
            btnRefrescar.Margin = new Padding(12, 3, 3, 3);
            btnRefrescar.Size = new System.Drawing.Size(148, 42);
            btnRefrescar.TabIndex = 1;
            btnRefrescar.Text = "Actualizar";
            btnRefrescar.UseVisualStyleBackColor = true;
            // 
            // lblLista
            // 
            lblLista.AutoSize = true;
            lblLista.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblLista.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblLista.Location = new System.Drawing.Point(24, 24);
            lblLista.Name = "lblLista";
            lblLista.Size = new System.Drawing.Size(181, 32);
            lblLista.TabIndex = 0;
            lblLista.Text = "Roles registrados";
            // 
            // detailCard
            // 
            detailCard.BackColor = System.Drawing.Color.White;
            detailCard.Controls.Add(detailLayout);
            detailCard.Controls.Add(lblDetalleTitulo);
            detailCard.Dock = DockStyle.Fill;
            detailCard.Location = new System.Drawing.Point(363, 3);
            detailCard.Name = "detailCard";
            detailCard.Padding = new Padding(24);
            detailCard.Size = new System.Drawing.Size(808, 606);
            detailCard.TabIndex = 1;
            //
            // btnToggleDetalles
            //
            btnToggleDetalles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnToggleDetalles.AutoSize = true;
            btnToggleDetalles.BackColor = System.Drawing.Color.White;
            btnToggleDetalles.FlatAppearance.BorderSize = 0;
            btnToggleDetalles.FlatStyle = FlatStyle.Flat;
            btnToggleDetalles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            btnToggleDetalles.ForeColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnToggleDetalles.Location = new System.Drawing.Point(138, 3);
            btnToggleDetalles.Name = "btnToggleDetalles";
            btnToggleDetalles.Padding = new Padding(12, 6, 12, 6);
            btnToggleDetalles.Margin = new Padding(12, 3, 3, 3);
            btnToggleDetalles.Size = new System.Drawing.Size(148, 38);
            btnToggleDetalles.TabIndex = 2;
            btnToggleDetalles.Text = "Ocultar detalles";
            btnToggleDetalles.UseVisualStyleBackColor = false;
            //
            // detailLayout
            //
            detailLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            detailLayout.ColumnCount = 2;
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailLayout.Controls.Add(lblEstado, 0, 0);
            detailLayout.Controls.Add(lblNombre, 0, 1);
            detailLayout.Controls.Add(txtNombre, 1, 1);
            detailLayout.Controls.Add(lblDetalleDescripcion, 0, 2);
            detailLayout.Controls.Add(txtDescripcion, 1, 2);
            detailLayout.Controls.Add(chkActivo, 1, 3);
            detailLayout.Controls.Add(lblCorreosTitulo, 0, 4);
            detailLayout.Controls.Add(emailsPanel, 1, 4);
            detailLayout.Controls.Add(lblModulos, 0, 5);
            detailLayout.Controls.Add(modulesPanel, 1, 5);
            detailLayout.Controls.Add(lblAyuda, 1, 6);
            detailLayout.Controls.Add(detailActions, 1, 7);
            detailLayout.Location = new System.Drawing.Point(24, 84);
            detailLayout.Name = "detailLayout";
            detailLayout.RowCount = 8;
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.Size = new System.Drawing.Size(760, 480);
            detailLayout.TabIndex = 1;
            // 
            // lblEstado
            // 
            lblEstado.AutoSize = true;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblEstado.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            lblEstado.Location = new System.Drawing.Point(3, 0);
            lblEstado.Name = "lblEstado";
            lblEstado.Padding = new Padding(0, 0, 0, 8);
            lblEstado.Size = new System.Drawing.Size(159, 31);
            lblEstado.TabIndex = 0;
            lblEstado.Text = "Estado: (sin selección)";
            detailLayout.SetColumnSpan(lblEstado, 2);
            // 
            // lblNombre
            // 
            lblNombre.AutoSize = true;
            lblNombre.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblNombre.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblNombre.Location = new System.Drawing.Point(3, 31);
            lblNombre.Name = "lblNombre";
            lblNombre.Padding = new Padding(0, 8, 0, 8);
            lblNombre.Size = new System.Drawing.Size(149, 39);
            lblNombre.TabIndex = 1;
            lblNombre.Text = "Nombre del rol";
            // 
            // txtNombre
            // 
            txtNombre.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtNombre.BorderStyle = BorderStyle.FixedSingle;
            txtNombre.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            txtNombre.Location = new System.Drawing.Point(163, 34);
            txtNombre.MaxLength = 80;
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new System.Drawing.Size(594, 30);
            txtNombre.TabIndex = 2;
            // 
            // lblDetalleDescripcion
            // 
            lblDetalleDescripcion.AutoSize = true;
            lblDetalleDescripcion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblDetalleDescripcion.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblDetalleDescripcion.Location = new System.Drawing.Point(3, 73);
            lblDetalleDescripcion.Name = "lblDetalleDescripcion";
            lblDetalleDescripcion.Padding = new Padding(0, 8, 0, 8);
            lblDetalleDescripcion.Size = new System.Drawing.Size(99, 39);
            lblDetalleDescripcion.TabIndex = 3;
            lblDetalleDescripcion.Text = "Descripción";
            // 
            // txtDescripcion
            // 
            txtDescripcion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDescripcion.BorderStyle = BorderStyle.FixedSingle;
            txtDescripcion.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            txtDescripcion.Location = new System.Drawing.Point(163, 76);
            txtDescripcion.MaxLength = 200;
            txtDescripcion.Multiline = true;
            txtDescripcion.Name = "txtDescripcion";
            txtDescripcion.ScrollBars = ScrollBars.Vertical;
            txtDescripcion.Size = new System.Drawing.Size(594, 80);
            txtDescripcion.TabIndex = 4;
            // 
            // chkActivo
            // 
            chkActivo.AutoSize = true;
            chkActivo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            chkActivo.Location = new System.Drawing.Point(163, 162);
            chkActivo.Name = "chkActivo";
            chkActivo.Padding = new Padding(0, 12, 0, 12);
            chkActivo.Size = new System.Drawing.Size(161, 49);
            chkActivo.TabIndex = 5;
            chkActivo.Text = "Rol habilitado";
            chkActivo.UseVisualStyleBackColor = true;
            //
            // lblCorreosTitulo
            //
            lblCorreosTitulo.AutoSize = true;
            lblCorreosTitulo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblCorreosTitulo.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblCorreosTitulo.Location = new System.Drawing.Point(3, 211);
            lblCorreosTitulo.Name = "lblCorreosTitulo";
            lblCorreosTitulo.Padding = new Padding(0, 8, 0, 8);
            lblCorreosTitulo.Size = new System.Drawing.Size(166, 39);
            lblCorreosTitulo.TabIndex = 6;
            lblCorreosTitulo.Text = "Correos asociados";
            //
            // emailsPanel
            //
            emailsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            emailsPanel.AutoSize = true;
            emailsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            emailsPanel.FlowDirection = FlowDirection.TopDown;
            emailsPanel.Location = new System.Drawing.Point(163, 214);
            emailsPanel.Margin = new Padding(3, 3, 3, 12);
            emailsPanel.Name = "emailsPanel";
            emailsPanel.Size = new System.Drawing.Size(594, 29);
            emailsPanel.TabIndex = 7;
            emailsPanel.WrapContents = false;
            //
            // lblModulos
            //
            lblModulos.AutoSize = true;
            lblModulos.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblModulos.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblModulos.Location = new System.Drawing.Point(3, 272);
            lblModulos.Name = "lblModulos";
            lblModulos.Padding = new Padding(0, 8, 0, 8);
            lblModulos.Size = new System.Drawing.Size(133, 39);
            lblModulos.TabIndex = 8;
            lblModulos.Text = "Módulos activos";
            //
            // modulesPanel
            //
            modulesPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            modulesPanel.AutoSize = true;
            modulesPanel.FlowDirection = FlowDirection.LeftToRight;
            modulesPanel.Location = new System.Drawing.Point(163, 275);
            modulesPanel.Margin = new Padding(3, 3, 3, 12);
            modulesPanel.Name = "modulesPanel";
            modulesPanel.Size = new System.Drawing.Size(594, 29);
            modulesPanel.TabIndex = 9;
            modulesPanel.WrapContents = true;
            //
            // lblAyuda
            //
            lblAyuda.AutoSize = true;
            lblAyuda.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            lblAyuda.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            lblAyuda.Location = new System.Drawing.Point(163, 316);
            lblAyuda.Margin = new Padding(3, 0, 3, 12);
            lblAyuda.Name = "lblAyuda";
            lblAyuda.Size = new System.Drawing.Size(383, 20);
            lblAyuda.TabIndex = 10;
            lblAyuda.Text = "Marca los módulos disponibles y habilita el rol para aprobarlo.";
            //
            // detailActions
            //
            detailActions.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            detailActions.AutoSize = true;
            detailActions.Controls.Add(btnGuardar);
            detailActions.Controls.Add(btnRefrescar);
            detailActions.FlowDirection = FlowDirection.LeftToRight;
            detailActions.Location = new System.Drawing.Point(163, 348);
            detailActions.Margin = new Padding(3, 0, 3, 0);
            detailActions.Name = "detailActions";
            detailActions.Size = new System.Drawing.Size(273, 46);
            detailActions.TabIndex = 11;
            detailActions.WrapContents = false;
            // 
            // btnGuardar
            // 
            btnGuardar.AutoSize = true;
            btnGuardar.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            btnGuardar.ForeColor = System.Drawing.Color.White;
            btnGuardar.Location = new System.Drawing.Point(3, 3);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Padding = new Padding(18, 10, 18, 10);
            btnGuardar.Size = new System.Drawing.Size(107, 40);
            btnGuardar.TabIndex = 0;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = false;
            // 
            // lblDetalleTitulo
            // 
            lblDetalleTitulo.AutoSize = true;
            lblDetalleTitulo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblDetalleTitulo.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            lblDetalleTitulo.Location = new System.Drawing.Point(24, 24);
            lblDetalleTitulo.Name = "lblDetalleTitulo";
            lblDetalleTitulo.Size = new System.Drawing.Size(202, 32);
            lblDetalleTitulo.TabIndex = 0;
            lblDetalleTitulo.Text = "Detalle del acceso";
            // 
            // Roles
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            ClientSize = new System.Drawing.Size(1180, 720);
            Controls.Add(layoutRoot);
            MinimumSize = new System.Drawing.Size(900, 600);
            Name = "Roles";
            Text = "Gestión de roles";
            layoutRoot.ResumeLayout(false);
            topBar.ResumeLayout(false);
            topBar.PerformLayout();
            contentLayout.ResumeLayout(false);
            rolesCard.ResumeLayout(false);
            rolesCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridRoles).EndInit();
            rolesActions.ResumeLayout(false);
            rolesActions.PerformLayout();
            detailCard.ResumeLayout(false);
            detailCard.PerformLayout();
            detailLayout.ResumeLayout(false);
            detailLayout.PerformLayout();
            detailActions.ResumeLayout(false);
            detailActions.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layoutRoot;
        private Panel topBar;
        private Label lblDescripcion;
        private Label lblTitulo;
        private TableLayoutPanel contentLayout;
        private Panel rolesCard;
        private Label lblLista;
        private FlowLayoutPanel rolesActions;
        private Button btnNuevo;
        private Button btnRefrescar;
        private DataGridView gridRoles;
        private Panel detailCard;
        private Label lblDetalleTitulo;
        private Button btnToggleDetalles;
        private TableLayoutPanel detailLayout;
        private Label lblEstado;
        private Label lblNombre;
        private TextBox txtNombre;
        private Label lblDetalleDescripcion;
        private TextBox txtDescripcion;
        private CheckBox chkActivo;
        private Label lblCorreosTitulo;
        private FlowLayoutPanel emailsPanel;
        private Label lblModulos;
        private FlowLayoutPanel modulesPanel;
        private Label lblAyuda;
        private FlowLayoutPanel detailActions;
        private Button btnGuardar;
    }
}
