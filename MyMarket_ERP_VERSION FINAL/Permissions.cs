using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyMarket_ERP
{
    public static class Permissions
    {
        private static readonly object _sync = new();
        private static Dictionary<string, HashSet<NavSection>>? _cache;

        private static readonly Dictionary<string, HashSet<NavSection>> Defaults =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["admin"] = new HashSet<NavSection>
                {
                    NavSection.Central,
                    NavSection.Compras,
                    NavSection.Clientes,
                    NavSection.Inventario,
                    NavSection.Contabilidad,
                    NavSection.Empleados,
                    NavSection.Roles
                },
                ["contable"] = new HashSet<NavSection>
                {
                    NavSection.Central,
                    NavSection.Contabilidad
                },
                ["caja"] = new HashSet<NavSection>
                {
                    NavSection.Central,
                    NavSection.Compras
                },
                ["inventario"] = new HashSet<NavSection>
                {
                    NavSection.Central,
                    NavSection.Inventario
                },
                ["cliente"] = new HashSet<NavSection>
                {
                    NavSection.Historial
                }
            };

        public static bool IsAllowed(string role, NavSection section)
        {
            var rules = GetRules();
            return rules.TryGetValue(role ?? string.Empty, out var set) && set.Contains(section);
        }

        public static void Reload()
        {
            lock (_sync)
            {
                _cache = null;
            }
        }

        public static IReadOnlyDictionary<string, HashSet<NavSection>> Snapshot()
        {
            var rules = GetRules();
            return rules.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<NavSection>(kvp.Value),
                StringComparer.OrdinalIgnoreCase
            );
        }

        private static Dictionary<string, HashSet<NavSection>> GetRules()
        {
            lock (_sync)
            {
                _cache ??= LoadFromDatabase();
                return _cache;
            }
        }

        private static Dictionary<string, HashSet<NavSection>> LoadFromDatabase()
        {
            var result = new Dictionary<string, HashSet<NavSection>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var cn = Database.OpenConnection();
                using var cmd = new SqlCommand(@"SELECT r.Name, rm.Module
                    FROM dbo.Roles r
                    LEFT JOIN dbo.RoleModules rm ON rm.RoleId = r.Id
                    WHERE r.IsActive = 1", cn);

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var role = rd.IsDBNull(0) ? null : rd.GetString(0);
                    if (string.IsNullOrWhiteSpace(role))
                        continue;

                    if (!result.TryGetValue(role, out var set))
                    {
                        set = new HashSet<NavSection>();
                        result[role] = set;
                    }

                    if (!rd.IsDBNull(1))
                    {
                        var moduleName = rd.GetString(1);
                        if (Enum.TryParse(moduleName, true, out NavSection section))
                        {
                            set.Add(section);
                        }
                    }
                }
            }
            catch (SqlException)
            {
                return CloneDefaults();
            }
            catch (InvalidOperationException)
            {
                return CloneDefaults();
            }

            if (result.Count == 0)
            {
                return CloneDefaults();
            }

            return result;
        }

        private static Dictionary<string, HashSet<NavSection>> CloneDefaults() =>
            Defaults.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<NavSection>(kvp.Value),
                StringComparer.OrdinalIgnoreCase
            );
    }
}
