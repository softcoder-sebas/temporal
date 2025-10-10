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
                BackColor = ModernTheme.Background,
                Name = "MainSplitContainer"
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
                Padding = new Padding(0)
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

            // Establecer el ancho inicial del sidebar basado en el estado global
            bool isCollapsed = SidebarControl.GlobalCollapsed ?? true;
            int initialDistance = isCollapsed ? SidebarControl.WIDTH_COLLAPSED : SidebarControl.WIDTH_EXPANDED;

            // Construir botones según rol y sección activa ANTES de establecer el SplitterDistance
            sidebar.Build(role, active);

            // Ahora sí establecer el SplitterDistance
            split.SplitterDistance = initialDistance;

            // Sincronizar ancho del panel izquierdo cuando colapsa/expande
            sidebar.SidebarWidthChanged += (s, w) =>
            {
                if (split.IsDisposed) return;
                try
                {
                    split.SplitterDistance = Math.Max(w, split.Panel1MinSize);
                    split.Panel1.Invalidate();
                    split.Panel2.Invalidate();
                    content.Invalidate();

                    // Forzar actualización del layout del contenido
                    foreach (Control ctrl in content.Controls)
                    {
                        ctrl.Invalidate();
                        if (ctrl is TableLayoutPanel tlp)
                        {
                            tlp.PerformLayout();
                        }
                    }
                }
                catch { }
            };

            // Navegación
            sidebar.SectionClicked += (s, section) =>
            {
                onNavigate?.Invoke(section);
            };

            host.Tag = active;

            // Forzar un layout completo
            host.ResumeLayout(true);
            host.PerformLayout();

            // Asegurar que todo se renderice correctamente
            split.ResumeLayout(true);
            split.PerformLayout();
            content.ResumeLayout(true);
            content.PerformLayout();

            // Refrescar después de un pequeño delay para asegurar que todo está listo
            host.Shown += (s, e) =>
            {
                split.SplitterDistance = initialDistance;
                split.Refresh();
                content.Refresh();
            };

            return content;
        }
    }
}