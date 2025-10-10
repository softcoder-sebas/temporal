using System;
using System.Collections.Generic;

namespace MyMarket_ERP
{
    public static class Permissions
    {
        private static readonly Dictionary<string, HashSet<NavSection>> Rules =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Admin: TODO
                ["admin"] = new HashSet<NavSection>
            {
                NavSection.Central, NavSection.Compras, NavSection.Clientes,
                NavSection.Inventario, NavSection.Contabilidad, NavSection.Empleados
            },

                // Contable: Central y Contabilidad
                ["contable"] = new HashSet<NavSection>
            {
                NavSection.Central, NavSection.Contabilidad
            },

                // Caja: Central y Compras (POS)
                ["caja"] = new HashSet<NavSection>
            {
                NavSection.Central, NavSection.Compras
            },

                // Inventario: Central e Inventario
                ["inventario"] = new HashSet<NavSection>
            {
                NavSection.Central, NavSection.Inventario
            },

                // Cliente: acceso únicamente al historial personal
                ["cliente"] = new HashSet<NavSection>
            {
                NavSection.Historial
            },
            };

        public static bool IsAllowed(string role, NavSection section) =>
            Rules.TryGetValue(role ?? "", out var set) && set.Contains(section);
    }
}
