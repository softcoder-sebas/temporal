using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public static class NavigationService
    {
        public static void Open(NavSection section, Form currentForm, string role)
        {
            // No reabrir la misma sección
            if (currentForm.Tag is NavSection tag && tag == section) return;

            // Guarda de permisos
            if (!Permissions.IsAllowed(role, section))
            {
                MessageBox.Show("No tienes permiso para abrir este módulo.",
                    "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Form? next = section switch
            {
                NavSection.Central => new Central(role) { Tag = NavSection.Central },
                NavSection.Compras => new POSCompras(AppSession.UserEmail) { Tag = NavSection.Compras },
                NavSection.Clientes => new Clientes() { Tag = NavSection.Clientes },
                NavSection.Historial => new Historial_facturacion() { Tag = NavSection.Historial },
                NavSection.Inventario => new Inventario() { Tag = NavSection.Inventario },
                NavSection.Contabilidad => new Contabilidad() { Tag = NavSection.Contabilidad },
                NavSection.Empleados => new Empleados() { Tag = NavSection.Empleados },
                _ => null
            };
            if (next == null) return;

            // Configurar el nuevo formulario para que coincida con el actual
            next.StartPosition = FormStartPosition.Manual;
            next.WindowState = currentForm.WindowState;

            if (currentForm.WindowState == FormWindowState.Normal)
            {
                next.Bounds = currentForm.Bounds;
            }
            else
            {
                // Si está maximizado, establecer bounds antes de maximizar
                next.Bounds = currentForm.RestoreBounds;
            }

            // Ocultar el formulario actual
            currentForm.Hide();

            // Configurar el evento de cierre
            next.FormClosed += (_, __) =>
            {
                if (!currentForm.IsDisposed)
                    currentForm.Close();
            };

            // Mostrar el nuevo formulario
            next.Show();

            // Forzar actualización del layout después de mostrar
            next.Refresh();
            next.PerformLayout();
        }
    }
}
