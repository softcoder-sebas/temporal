using System;
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

            next.StartPosition = FormStartPosition.Manual;
            next.Bounds = currentForm.Bounds;

            currentForm.Hide();
            next.FormClosed += (_, __) => currentForm.Close();
            next.Show();
        }
    }
}
