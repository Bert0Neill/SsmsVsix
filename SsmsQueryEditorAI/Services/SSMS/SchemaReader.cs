using SSMS_AI_Buddy.Models;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
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

        //public static string GenerateDatabaseSchemaJson(string connectionString)
        //{
        //    var schema = new CachedSchema
        //    {
        //        Tables = new List<TableInfo>(),
        //        Columns = new List<ColumnInfo>(),
        //        PrimaryKeys = new List<PrimaryKeyInfo>(),
        //        ForeignKeys = new List<ForeignKeyInfo>(),
        //        Indexes = new List<IndexInfo>()
        //    };

        //    using (var conn = new SqlConnection(connectionString))
        //    {
        //        conn.Open();

        //        // 1. Tables
        //        const string tablesSql = @"
        //            SELECT s.name AS SchemaName, t.name AS TableName
        //            FROM sys.tables t
        //            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        //            WHERE t.is_ms_shipped = 0
        //            ORDER BY s.name, t.name";

        //        using (var cmd = new SqlCommand(tablesSql, conn))
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //                schema.Tables.Add(new TableInfo
        //                {
        //                    Schema = rdr.GetString(0),
        //                    Name = rdr.GetString(1)
        //                });
        //        }

        //        // 2. Columns
        //        const string columnsSql = @"
        //            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, ty.name AS DataType
        //            FROM sys.columns c
        //            INNER JOIN sys.tables t ON c.object_id = t.object_id
        //            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        //            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
        //            WHERE t.is_ms_shipped = 0
        //            ORDER BY s.name, t.name, c.column_id";

        //        using (var cmd = new SqlCommand(columnsSql, conn))
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //                schema.Columns.Add(new ColumnInfo
        //                {
        //                    Schema = rdr.GetString(0),
        //                    TableName = rdr.GetString(1),
        //                    Name = rdr.GetString(2),
        //                    DataType = rdr.GetString(3)
        //                });
        //        }

        //        // 3. Primary Keys
        //        const string pkSql = @"
        //            SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
        //            FROM sys.indexes i
        //            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        //            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        //            INNER JOIN sys.tables t ON t.object_id = i.object_id
        //            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        //            WHERE i.is_primary_key = 1
        //            ORDER BY s.name, t.name, ic.key_ordinal";

        //        using (var cmd = new SqlCommand(pkSql, conn))
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //                schema.PrimaryKeys.Add(new PrimaryKeyInfo
        //                {
        //                    Schema = rdr.GetString(0),
        //                    Table = rdr.GetString(1),
        //                    Column = rdr.GetString(2)
        //                });
        //        }

        //        // 4. Foreign Keys
        //        const string fkSql = @"
        //            SELECT 
        //                sch_child.name AS ChildSchema,
        //                tp.name AS ChildTable,
        //                cp.name AS ChildColumn,
        //                sch_parent.name AS ParentSchema,
        //                tr.name AS ParentTable,
        //                cr.name AS ParentColumn
        //            FROM sys.foreign_keys fk
        //            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        //            INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
        //            INNER JOIN sys.schemas sch_child ON tp.schema_id = sch_child.schema_id
        //            INNER JOIN sys.columns cp ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
        //            INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
        //            INNER JOIN sys.schemas sch_parent ON tr.schema_id = sch_parent.schema_id
        //            INNER JOIN sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
        //            ORDER BY ChildSchema, ChildTable, fk.name";

        //        using (var cmd = new SqlCommand(fkSql, conn))
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //                schema.ForeignKeys.Add(new ForeignKeyInfo
        //                {
        //                    Schema = rdr.GetString(0),
        //                    Table = rdr.GetString(1),
        //                    Column = rdr.GetString(2),
        //                    ReferencedSchema = rdr.GetString(3),
        //                    ReferencedTable = rdr.GetString(4),
        //                    ReferencedColumn = rdr.GetString(5)
        //                });
        //        }

        //        // 5. Indexes (excluding PKs and unique constraints)
        //        const string idxSql = @"
        //            SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName, i.is_unique, i.type_desc,
        //                   c.name AS ColumnName, ic.is_included_column
        //            FROM sys.indexes i
        //            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        //            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        //            INNER JOIN sys.tables t ON t.object_id = i.object_id
        //            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        //            WHERE i.is_primary_key = 0
        //              AND i.is_unique_constraint = 0
        //              AND t.is_ms_shipped = 0
        //            ORDER BY s.name, t.name, i.name, ic.key_ordinal";

        //        var indexDict = new Dictionary<string, IndexInfo>();

        //        using (var cmd = new SqlCommand(idxSql, conn))
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //            {
        //                var key = rdr.GetString(0) + "|" + rdr.GetString(2); // Schema|IndexName

        //                if (!indexDict.ContainsKey(key))
        //                {
        //                    indexDict[key] = new IndexInfo
        //                    {
        //                        Schema = rdr.GetString(0),
        //                        Table = rdr.GetString(1),
        //                        Name = rdr.GetString(2),
        //                        IsUnique = rdr.GetBoolean(3),
        //                        IsClustered = rdr.GetString(4).Contains("CLUSTERED"),
        //                        Columns = new List<string>(),
        //                        IncludedColumns = new List<string>()
        //                    };
        //                }

        //                if (rdr.GetBoolean(6))
        //                    indexDict[key].IncludedColumns.Add(rdr.GetString(5));
        //                else
        //                    indexDict[key].Columns.Add(rdr.GetString(5));
        //            }
        //        }

        //        schema.Indexes = new List<IndexInfo>(indexDict.Values);
        //    }

        //    // Return JSON
        //    var options = new JsonSerializerOptions
        //    {
        //        WriteIndented = true
        //    };
        //    return JsonSerializer.Serialize(schema, options);
        //}

        #region Queries

        private static List<TableInfo> GetTables(SqlConnection conn)
        {
            const string sql = @"
                SELECT s.name AS SchemaName, t.name AS TableName
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE t.is_ms_shipped = 0";

            var list = new List<TableInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new TableInfo
                    {
                        Schema = rdr.GetString(0),
                        Name = rdr.GetString(1)
                    });
                }
            }

            return list;
        }

        private static List<ColumnInfo> GetColumns(SqlConnection conn)
        {
            const string sql = @"
                SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, ty.name AS DataType
                FROM sys.columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                WHERE t.is_ms_shipped = 0";

            var list = new List<ColumnInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new ColumnInfo
                    {
                        Schema = rdr.GetString(0),
                        TableName = rdr.GetString(1),
                        Name = rdr.GetString(2),
                        DataType = rdr.GetString(3)
                    });
                }
            }

            return list;
        }

        private static List<PrimaryKeyInfo> GetPrimaryKeys(SqlConnection conn)
        {
            const string sql = @"
                SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                INNER JOIN sys.tables t ON t.object_id = i.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE i.is_primary_key = 1";

            var list = new List<PrimaryKeyInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new PrimaryKeyInfo
                    {
                        Schema = rdr.GetString(0),
                        Table = rdr.GetString(1),
                        Column = rdr.GetString(2)
                    });
                }
            }

            return list;
        }

        private static List<ForeignKeyInfo> GetForeignKeys(SqlConnection conn)
        {
            const string sql = @"
                SELECT 
                    sch_child.name AS ChildSchema,
                    tp.name AS ChildTable,
                    cp.name AS ChildColumn,
                    sch_parent.name AS ParentSchema,
                    tr.name AS ParentTable,
                    cr.name AS ParentColumn
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
                INNER JOIN sys.schemas sch_child ON sch_child.schema_id = tp.schema_id
                INNER JOIN sys.columns cp ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
                INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
                INNER JOIN sys.schemas sch_parent ON sch_parent.schema_id = tr.schema_id
                INNER JOIN sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
                ORDER BY ChildSchema, ChildTable, fk.name";

            var list = new List<ForeignKeyInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new ForeignKeyInfo
                    {
                        Schema = rdr.GetString(rdr.GetOrdinal("ChildSchema")),
                        Table = rdr.GetString(rdr.GetOrdinal("ChildTable")),
                        Column = rdr.GetString(rdr.GetOrdinal("ChildColumn")),
                        ReferencedSchema = rdr.GetString(rdr.GetOrdinal("ParentSchema")),
                        ReferencedTable = rdr.GetString(rdr.GetOrdinal("ParentTable")),
                        ReferencedColumn = rdr.GetString(rdr.GetOrdinal("ParentColumn"))
                    });
                }
            }

            return list;
        }

        private static List<IndexInfo> GetIndexes(SqlConnection conn)
        {
            const string sql = @"
                SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName, i.is_unique, i.type_desc,
                       c.name AS ColumnName, ic.is_included_column
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE i.is_primary_key = 0
                  AND i.is_unique_constraint = 0
                  AND t.is_ms_shipped = 0";

            var dict = new Dictionary<string, IndexInfo>();

            using (var cmd = new SqlCommand(sql, conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var key = rdr.GetString(0) + "|" + rdr.GetString(2); // Schema|IndexName

                    if (!dict.ContainsKey(key))
                    {
                        dict[key] = new IndexInfo
                        {
                            Schema = rdr.GetString(0),
                            Table = rdr.GetString(1),
                            Name = rdr.GetString(2),
                            IsUnique = rdr.GetBoolean(3),
                            IsClustered = rdr.GetString(4).Contains("CLUSTERED"),
                            Columns = new List<string>(),
                            IncludedColumns = new List<string>()
                        };
                    }

                    if (rdr.GetBoolean(6))
                        dict[key].IncludedColumns.Add(rdr.GetString(5));
                    else
                        dict[key].Columns.Add(rdr.GetString(5));
                }
            }

            return dict.Values.ToList();
        }

        #endregion
    }
}
