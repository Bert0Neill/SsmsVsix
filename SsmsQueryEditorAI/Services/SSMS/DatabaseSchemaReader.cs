using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public class DatabaseSchemaReader
    {
        public static string ReadSchema(
            string server,
            string database,
            string username = null,
            string password = null)
        {
            var cs = username == null
                ? $"Server={server};Database={database};Trusted_Connection=True;"
                : $"Server={server};Database={database};User Id={username};Password={password};";

            var sb = new StringBuilder();

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                var tables = GetTables(conn);
                var columns = GetColumns(conn);
                var primaryKeys = GetPrimaryKeys(conn);
                var foreignKeys = GetForeignKeys(conn);

                foreach (var table in tables)
                {
                    var alias = table.Name.Substring(0, 1).ToLower();

                    sb.AppendLine($"Table {table.Name} (alias {alias})");
                    sb.AppendLine("Columns:");

                    foreach (var col in columns.Where(c => c.TableName == table.Name))
                    {
                        var line = $"- {col.Name} {col.DataType}";

                        if (primaryKeys.Any(pk => pk.Table == table.Name && pk.Column == col.Name))
                            line += " PK";

                        var fk = foreignKeys.FirstOrDefault(f =>
                            f.Table == table.Name && f.Column == col.Name);

                        if (fk != null)
                            line += $" FK -> {fk.ReferencedTable}.{fk.ReferencedColumn}";

                        sb.AppendLine(line);
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        #region Queries

        private static List<TableInfo> GetTables(SqlConnection conn)
        {
            const string sql = @"
            SELECT name
            FROM sys.tables
            WHERE is_ms_shipped = 0
            ORDER BY name";

            var list = new List<TableInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new TableInfo
                    {
                        Name = rdr.GetString(0)
                    });
                }
            }

            return list;
        }

        private static List<ColumnInfo> GetColumns(SqlConnection conn)
        {
            const string sql = @"
            SELECT
                t.name AS TableName,
                c.name AS ColumnName,
                ty.name AS DataType
            FROM sys.columns c
            JOIN sys.tables t ON c.object_id = t.object_id
            JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            WHERE t.is_ms_shipped = 0
            ORDER BY t.name, c.column_id";

            var list = new List<ColumnInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new ColumnInfo
                    {
                        TableName = rdr.GetString(0),
                        Name = rdr.GetString(1),
                        DataType = rdr.GetString(2)
                    });
                }
            }

            return list;
        }

        private static List<PrimaryKeyInfo> GetPrimaryKeys(SqlConnection conn)
        {
            const string sql = @"
            SELECT
                t.name AS TableName,
                c.name AS ColumnName
            FROM sys.indexes i
            JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            JOIN sys.tables t ON t.object_id = i.object_id
            WHERE i.is_primary_key = 1";

            var list = new List<PrimaryKeyInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new PrimaryKeyInfo
                    {
                        Table = rdr.GetString(0),
                        Column = rdr.GetString(1)
                    });
                }
            }

            return list;
        }

        private static List<ForeignKeyInfo> GetForeignKeys(SqlConnection conn)
        {
            const string sql = @"
            SELECT
                tp.name AS TableName,
                cp.name AS ColumnName,
                tr.name AS ReferencedTable,
                cr.name AS ReferencedColumn
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
            JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
            JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
            JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id";

            var list = new List<ForeignKeyInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new ForeignKeyInfo
                    {
                        Table = rdr.GetString(0),
                        Column = rdr.GetString(1),
                        ReferencedTable = rdr.GetString(2),
                        ReferencedColumn = rdr.GetString(3)
                    });
                }
            }

            return list;
        }

        #endregion

        #region Models

        private class TableInfo
        {
            public string Name { get; set; }
        }

        private class ColumnInfo
        {
            public string TableName { get; set; }
            public string Name { get; set; }
            public string DataType { get; set; }
        }

        private class PrimaryKeyInfo
        {
            public string Table { get; set; }
            public string Column { get; set; }
        }

        private class ForeignKeyInfo
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string ReferencedTable { get; set; }
            public string ReferencedColumn { get; set; }
        }

        #endregion
    }
}
