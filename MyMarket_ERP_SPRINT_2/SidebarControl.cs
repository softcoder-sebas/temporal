using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

//SR3
namespace MyMarket_ERP
{
    /// <summary>
    /// Barra lateral con colapso/expansión y estado global persistente entre formularios.
    /// Compatible con SidebarInstaller (Build, SectionClicked, SidebarWidthChanged).
    /// </summary>
    public class SidebarControl : UserControl
    {
        // ====== Estado global para persistir al navegar (true = colapsado) ======
        public static bool? GlobalCollapsed = null;

        // ====== Dimensiones ======
        public const int WIDTH_COLLAPSED = 72;
        public const int WIDTH_EXPANDED = 240;

        // ====== Eventos usados por SidebarInstaller ======
        public event EventHandler<int>? SidebarWidthChanged;        // notifica nuevo ancho
        public event EventHandler<NavSection>? SectionClicked;      // navegación

        // ====== UI ======
        private readonly Panel _root;
        public Panel PanelNav { get; private set; } = default!;
        private readonly Panel _footer;
        private readonly Button _btnToggle;
        private readonly ToolTip _tips = new ToolTip();

        // ====== Modelo ======
        private readonly List<SidebarButton> _buttons = new();
        private readonly Dictionary<NavSection, SidebarButton> _bySection = new();

        // ====== Estado ======
        private bool _collapsed = true;

        public SidebarControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.White;

            // ---- Layout principal (Grid: Nav + Footer) ----
            _root = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // nav
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));    // footer

            PanelNav = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(8)   // deja aire para que no se pegue a los bordes
            };

            _footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8)
            };

            _btnToggle = new Button
            {
                Dock = DockStyle.Fill,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.WhiteSmoke
            };
            _btnToggle.Click += (s, e) => SetCollapsed(!_collapsed, notify: true);

            _footer.Controls.Add(_btnToggle);

            grid.Controls.Add(PanelNav, 0, 0);
            grid.Controls.Add(_footer, 0, 1);

            _root.Controls.Add(grid);
            Controls.Add(_root);

            // Estado inicial: usa el estado global si existe
            bool startCollapsed = GlobalCollapsed ?? true;
            ApplyCollapsedState(startCollapsed, notify: false);
        }

        // --------------------------- API pública ---------------------------

        /// <summary>Construye los botones según el rol y marca la sección activa.</summary>
        public void Build(string role, NavSection active)
        {
            SuspendLayout();
            PanelNav.SuspendLayout();

            // Limpia estado anterior si hubiese
            foreach (Control c in PanelNav.Controls.Cast<Control>().ToArray())
                c.Dispose();
            PanelNav.Controls.Clear();
            _buttons.Clear();
            _bySection.Clear();

            // Helper para crear/agregar
            SidebarButton Add(NavSection section, string title, string glyph, Action? customAction = null)
            {
                var btn = new SidebarButton
                {
                    TitleText = title,
                    IconGlyph = glyph,
                    Dock = DockStyle.Top,
                    Width = _collapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED
                };
                btn.SetCollapsed(_collapsed);

                if (customAction != null)
                {
                    // Acción personalizada (para cerrar sesión)
                    btn.Click += (s, e) => customAction();
                }
                else
                {
                    // Acción de navegación normal
                    btn.Click += (s, e) =>
                    {
                        SetActive(section);
                        SectionClicked?.Invoke(this, section);
                    };
                }

                // ToolTip cuando está colapsado
                _tips.SetToolTip(btn, title);

                // Lo apilamos arriba (DockStyle.Top apila en reversa)
                PanelNav.Controls.Add(btn);
                PanelNav.Controls.SetChildIndex(btn, 0);

                _buttons.Add(btn);
                _bySection[section] = btn;
                return btn;
            }

            // Verifica si es rol cliente
            bool isCliente = string.Equals(role, "cliente", StringComparison.OrdinalIgnoreCase);

            if (isCliente)
            {
                // Para clientes: solo mostrar Mis compras y Cerrar sesión
                Add(NavSection.Historial, "Mis compras", IconGlyphs.History);

                // Botón de cerrar sesión con acción personalizada
                var btnLogout = new SidebarButton
                {
                    TitleText = "Cerrar sesión",
                    IconGlyph = IconGlyphs.Logout,
                    Dock = DockStyle.Top,
                    Width = _collapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED,
                    // Estilo especial para cerrar sesión
                    BgNormal = Color.FromArgb(254, 242, 242),
                    BgHover = Color.FromArgb(254, 226, 226),
                    BgActive = Color.FromArgb(220, 38, 38),
                    FgNormal = Color.FromArgb(185, 28, 28),
                    FgActive = Color.White
                };
                btnLogout.SetCollapsed(_collapsed);
                btnLogout.Click += (s, e) => CerrarSesion();

                _tips.SetToolTip(btnLogout, "Cerrar sesión");

                PanelNav.Controls.Add(btnLogout);
                PanelNav.Controls.SetChildIndex(btnLogout, 0);
                _buttons.Add(btnLogout);
            }
            else
            {
                // Para otros roles: menú completo
                bool Allowed(NavSection s) => Permissions.IsAllowed(role, s);

                if (Allowed(NavSection.Central)) Add(NavSection.Central, "Dashboard", IconGlyphs.Dashboard);
                if (Allowed(NavSection.Compras)) Add(NavSection.Compras, "Compras", IconGlyphs.Cart);
                if (Allowed(NavSection.Clientes)) Add(NavSection.Clientes, "Clientes", IconGlyphs.PeopleContact);
                if (Allowed(NavSection.Inventario)) Add(NavSection.Inventario, "Inventario", IconGlyphs.Boxes);
                if (Allowed(NavSection.Contabilidad)) Add(NavSection.Contabilidad, "Contabilidad", IconGlyphs.Calculator);
                if (Allowed(NavSection.Empleados)) Add(NavSection.Empleados, "Empleados", IconGlyphs.PeopleTeam);
                if (Allowed(NavSection.Roles)) Add(NavSection.Roles, "Roles", IconGlyphs.Shield);
            }

            // Marca activa (solo si no es cliente o si es el historial)
            if (!isCliente || active == NavSection.Historial)
            {
                SetActive(active);
            }

            // Acomoda apilado y tamaños
            RepositionButtons();

            PanelNav.ResumeLayout(true);
            ResumeLayout(true);

            // Asegura que el host reciba el ancho correcto al terminar de construir
            SidebarWidthChanged?.Invoke(this, _collapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED);
        }

        // Agrega este método al final de la clase SidebarControl
        private void CerrarSesion()
        {
            var result = MessageBox.Show(
                "¿Deseas cerrar sesión?",
                "Cerrar sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Limpiar la sesión
                AppSession.Clear();

                // Encontrar y cerrar el formulario actual
                var currentForm = FindForm();
                if (currentForm != null)
                {
                    currentForm.Hide();

                    // Mostrar el login
                    var login = Application.OpenForms.OfType<Login>().FirstOrDefault();
                    if (login != null && !login.IsDisposed)
                    {
                        login.Show();
                        login.Activate();
                    }
                    else
                    {
                        // Si no existe el login, crear uno nuevo
                        var newLogin = new Login();
                        newLogin.Show();
                    }

                    currentForm.Close();
                }
            }
        }

        /// <summary>Indica si está colapsada.</summary>
        [Browsable(false)]
        public bool IsCollapsed => _collapsed;

        /// <summary>Colapsa/expande (sin animación).</summary>
        public void SetCollapsed(bool collapsed, bool notify = true)
        {
            if (_collapsed == collapsed) return;
            ApplyCollapsedState(collapsed, notify);
        }

        // --------------------------- Núcleo de estado ---------------------------

        private void ApplyCollapsedState(bool collapsed, bool notify)
        {
            bool stateChanged = (_collapsed != collapsed);
            _collapsed = collapsed;
            GlobalCollapsed = collapsed; // persistir globalmente

            int targetWidth = collapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED;

            SuspendLayout();
            PanelNav.SuspendLayout();

            // Ajustar ancho propio
            Width = targetWidth;
            MinimumSize = new Size(targetWidth, 0);
            MaximumSize = new Size(targetWidth, int.MaxValue);

            // Botón de toggle (⮞ expandir / ⮜ minimizar)
            _btnToggle.Text = collapsed ? "⮞" : "⮜  Minimizar";
            _tips.SetToolTip(_btnToggle, collapsed ? "Expandir" : "Contraer");

            // Ajustar todos los botones
            foreach (var it in _buttons)
            {
                it.SuspendLayout();
                it.Width = targetWidth;
                it.SetCollapsed(collapsed);
                it.ResumeLayout(true);
                it.Refresh();
            }

            RepositionButtons();

            PanelNav.ResumeLayout(true);
            ResumeLayout(true);

            // Forzar actualización visual completa
            Invalidate(true);
            Update();
            Refresh();

            // Notifica al host (SplitContainer en SidebarInstaller)
            if (notify && stateChanged)
            {
                SidebarWidthChanged?.Invoke(this, targetWidth);
            }
        }

        /// <summary>Acomoda orden de apilado: botones normales arriba.</summary>
        private void RepositionButtons()
        {
            PanelNav.SuspendLayout();

            // Reapila normales (último agregado queda arriba)
            foreach (var b in _buttons.Where(b => !b.IsDisposed))
            {
                b.Dock = DockStyle.Top;
                if (!PanelNav.Controls.Contains(b))
                    PanelNav.Controls.Add(b);
                PanelNav.Controls.SetChildIndex(b, 0);
            }

            PanelNav.ResumeLayout(true);
        }

        // --------------------------- Utilidades ---------------------------

        private void SetActive(NavSection section)
        {
            foreach (var kv in _bySection)
                kv.Value.SetActive(kv.Key == section);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            // Respeta el estado global en cuanto se crea el control
            bool startCollapsed = GlobalCollapsed ?? true;
            ApplyCollapsedState(startCollapsed, notify: true);
        }
    }
}