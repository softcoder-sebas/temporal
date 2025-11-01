using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyMarket_ERP
{
    public static class RolesRepository
    {
        public static List<RoleDefinition> GetAll()
        {
            var map = new Dictionary<int, RoleDefinition>();

            using var cn = Database.OpenConnection();
            using var cmd = new SqlCommand(@"SELECT r.Id, r.Name, r.Description, r.IsActive, r.IsSystem, rm.Module
                    FROM dbo.Roles r
                    LEFT JOIN dbo.RoleModules rm ON rm.RoleId = r.Id
                    ORDER BY r.Name", cn);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var id = rd.GetInt32(0);

                if (!map.TryGetValue(id, out var role))
                {
                    role = new RoleDefinition
                    {
                        Id = id,
                        Name = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                        Description = rd.IsDBNull(2) ? null : rd.GetString(2),
                        IsActive = !rd.IsDBNull(3) && rd.GetBoolean(3),
                        IsSystem = !rd.IsDBNull(4) && rd.GetBoolean(4)
                    };

                    map[id] = role;
                }

                if (!rd.IsDBNull(5))
                {
                    var moduleName = rd.GetString(5);
                    if (Enum.TryParse(moduleName, true, out NavSection section))
                    {
                        role.Modules.Add(section);
                    }
                }
            }

            return map.Values
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void Save(RoleDefinition role, bool allowRename)
        {
            if (role is null) throw new ArgumentNullException(nameof(role));

            using var cn = Database.OpenConnection();
            using var tx = cn.BeginTransaction();

            try
            {
                if (role.Id == 0)
                {
                    using var insert = new SqlCommand(@"INSERT INTO dbo.Roles(Name, Description, IsActive, IsSystem)
                        VALUES(@name, @desc, @active, 0);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx);

                    insert.Parameters.AddWithValue("@name", role.Name);
                    insert.Parameters.AddWithValue("@desc", (object?)role.Description ?? DBNull.Value);
                    insert.Parameters.AddWithValue("@active", role.IsActive);

                    role.Id = (int)insert.ExecuteScalar();
                }
                else
                {
                    var sql = allowRename
                        ? @"UPDATE dbo.Roles SET Name=@name, Description=@desc, IsActive=@active WHERE Id=@id;"
                        : @"UPDATE dbo.Roles SET Description=@desc, IsActive=@active WHERE Id=@id;";

                    using var update = new SqlCommand(sql, cn, tx);
                    if (allowRename)
                    {
                        update.Parameters.AddWithValue("@name", role.Name);
                    }
                    update.Parameters.AddWithValue("@desc", (object?)role.Description ?? DBNull.Value);
                    update.Parameters.AddWithValue("@active", role.IsActive);
                    update.Parameters.AddWithValue("@id", role.Id);
                    update.ExecuteNonQuery();
                }

                using (var deleteModules = new SqlCommand("DELETE FROM dbo.RoleModules WHERE RoleId=@id;", cn, tx))
                {
                    deleteModules.Parameters.AddWithValue("@id", role.Id);
                    deleteModules.ExecuteNonQuery();
                }

                foreach (var module in role.Modules.Distinct())
                {
                    using var insertModule = new SqlCommand(@"INSERT INTO dbo.RoleModules(RoleId, Module)
                        VALUES(@id, @module);", cn, tx);
                    insertModule.Parameters.AddWithValue("@id", role.Id);
                    insertModule.Parameters.AddWithValue("@module", module.ToString());
                    insertModule.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
