using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public static class SidebarInstaller
    {
        public static Panel Install(Form host, string role, NavSection active, Action<NavSection> onNavigate)
        {
            host.SuspendLayout();

            host.BackColor = ModernTheme.Background;
            host.MinimumSize = new Size(1280, 768);
            host.WindowState = FormWindowState.Maximized;
            host.FormBorderStyle = FormBorderStyle.Sizable;
            host.MaximizeBox = true;

            // Capturar controles existentes del formulario
            var existingControls = host.Controls.Cast<Control>().ToList();

            // Split principal (sidebar + contenido)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                SplitterWidth = 1,
                Panel1MinSize = 72,
                BackColor = ModernTheme.Background
            };
            host.Controls.Clear();
            host.Controls.Add(split);

            // Sidebar
            var sidebar = new SidebarControl { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(sidebar);

            // Contenedor de contenido (sin padding extra, sin headers adicionales)
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.Background,
                Name = "ContentPanel",
                AutoScroll = true,
                Padding = new Padding(0) // Sin padding para evitar barras blancas
            };
            split.Panel2.Controls.Add(content);

            // Mover los controles originales directamente al panel de contenido
            foreach (var ctrl in existingControls)
            {
                content.Controls.Add(ctrl);
                if (ctrl is TableLayoutPanel || ctrl is Panel)
                {
                    ctrl.Dock = DockStyle.Fill;
                }
            }

            // Sidebar colapsada al iniciar
            split.SplitterDistance = 72;

            // Construir botones según rol y sección activa
            sidebar.Build(role, active);

            // Sincronizar ancho del panel izquierdo cuando colapsa/expande
            sidebar.SidebarWidthChanged += (s, w) =>
            {
                split.SplitterDistance = Math.Max(w, split.Panel1MinSize);
                split.Panel1.Refresh();
            };

            // Navegación
            sidebar.SectionClicked += (s, section) =>
            {
                onNavigate?.Invoke(section);
            };

            sidebar.LogoutRequested += (s, _) =>
            {
                AppSession.Clear();
                host.Close();
            };

            host.Tag = active;

            host.ResumeLayout(true);
            host.PerformLayout();

            return content;
        }
    }
}