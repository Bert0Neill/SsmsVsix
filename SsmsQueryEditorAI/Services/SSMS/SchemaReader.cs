using SSMS_AI_Buddy.Models;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class SchemaReader
    {
        public static CachedSchema LoadSchemaFromDatabase(string connectionString)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                return new CachedSchema
                {
                    Tables = GetTables(conn),
                    Columns = GetColumns(conn),
                    PrimaryKeys = GetPrimaryKeys(conn),
                    ForeignKeys = GetForeignKeys(conn),
                    Indexes = GetIndexes(conn)
                };
            }
        }

        #region Queries

        private static List<TableInfo> GetTables(SqlConnection conn)
        {
            const string sql = @"
                SELECT name
                FROM sys.tables
                WHERE is_ms_shipped = 0";

            var list = new List<TableInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new TableInfo { Name = rdr.GetString(0) });
                }
            }

            return list;
        }

        private static List<ColumnInfo> GetColumns(SqlConnection conn)
        {
            const string sql = @"
                SELECT t.name, c.name, ty.name
                FROM sys.columns c
                JOIN sys.tables t ON c.object_id = t.object_id
                JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                WHERE t.is_ms_shipped = 0";

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
                SELECT t.name, c.name
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
                SELECT tp.name, cp.name, tr.name, cr.name
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

        private static List<IndexInfo> GetIndexes(SqlConnection conn)
        {
            const string sql = @"
                SELECT t.name, i.name, i.is_unique, i.type_desc,
                       c.name, ic.is_included_column
                FROM sys.indexes i
                JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                JOIN sys.tables t ON i.object_id = t.object_id
                WHERE i.is_primary_key = 0
                  AND i.is_unique_constraint = 0
                  AND t.is_ms_shipped = 0";

            var dict = new Dictionary<string, IndexInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var key = rdr.GetString(0) + "|" + rdr.GetString(1);

                    if (!dict.ContainsKey(key))
                    {
                        dict[key] = new IndexInfo
                        {
                            Table = rdr.GetString(0),
                            Name = rdr.GetString(1),
                            IsUnique = rdr.GetBoolean(2),
                            IsClustered = rdr.GetString(3).Contains("CLUSTERED"),
                            Columns = new List<string>(),
                            IncludedColumns = new List<string>()
                        };
                    }

                    if (rdr.GetBoolean(5))
                        dict[key].IncludedColumns.Add(rdr.GetString(4));
                    else
                        dict[key].Columns.Add(rdr.GetString(4));
                }
            }

            return dict.Values.ToList();
        }

        #endregion
    }
}
