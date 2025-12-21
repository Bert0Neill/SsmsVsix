//using SSMS_AI_Buddy.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using Newtonsoft.Json;

//namespace SSMS_AI_Buddy.Services.SSMS
//{
//    public static class SchemaReader
//    {
//        public static string LoadSchemaJson(string connectionString)
//        {
//            var databaseSchema = new DatabaseSchema();

//            using (var conn = new SqlConnection(connectionString))
//            {
//                conn.Open();
//                databaseSchema.Database = conn.Database;

//                var cmd = new SqlCommand(@"
//                    SELECT 
//                        s.name AS SchemaName,
//                        t.name AS TableName,
//                        c.name AS ColumnName,
//                        ty.name AS DataType,
//                        CASE WHEN pkCols.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
//                        fkCols.constraint_schema AS FK_Schema,
//                        fkPk.TABLE_NAME AS FK_Table,
//                        fkPk.COLUMN_NAME AS FK_Column,
//                        ep.value AS CustomType
//                    FROM sys.tables t
//                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
//                    INNER JOIN sys.columns c ON c.object_id = t.object_id
//                    INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
//                    LEFT JOIN
//                        (SELECT ic.object_id, ic.column_id
//                         FROM sys.indexes i
//                         INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
//                         WHERE i.is_primary_key = 1) pkCols
//                    ON c.object_id = pkCols.object_id AND c.column_id = pkCols.column_id
//                    LEFT JOIN
//                        (SELECT fkCols.constraint_name, fkCols.constraint_schema, fkCols.table_name, fkCols.column_name,
//                                pkCols.table_name AS PKTable, pkCols.column_name AS PKColumn
//                         FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS fk
//                         JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fkCols ON fk.CONSTRAINT_NAME = fkCols.CONSTRAINT_NAME
//                         JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pkCols ON fk.UNIQUE_CONSTRAINT_NAME = pkCols.CONSTRAINT_NAME) fkCols
//                    ON t.name = fkCols.table_name AND c.name = fkCols.column_name
//                    LEFT JOIN sys.extended_properties ep
//                    ON ep.major_id = t.object_id AND ep.minor_id = c.column_id AND ep.name = 'CustomType'
//                    WHERE t.is_ms_shipped = 0
//                    ORDER BY s.name, t.name, c.column_id", conn);

//                var tableDict = new Dictionary<string, TableInfo>();

//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        string schema = reader.GetString(reader.GetOrdinal("SchemaName"));
//                        string tableName = reader.GetString(reader.GetOrdinal("TableName"));
//                        string columnName = reader.GetString(reader.GetOrdinal("ColumnName"));
//                        string dataType = reader.GetString(reader.GetOrdinal("DataType"));
//                        bool isPk = reader.GetInt32(reader.GetOrdinal("IsPrimaryKey")) == 1;
//                        string fkSchema = reader.IsDBNull(reader.GetOrdinal("FK_Schema")) ? null : reader.GetString(reader.GetOrdinal("FK_Schema"));
//                        string fkTable = reader.IsDBNull(reader.GetOrdinal("FK_Table")) ? null : reader.GetString(reader.GetOrdinal("FK_Table"));
//                        string fkColumn = reader.IsDBNull(reader.GetOrdinal("FK_Column")) ? null : reader.GetString(reader.GetOrdinal("FK_Column"));
//                        string customType = reader.IsDBNull(reader.GetOrdinal("CustomType")) ? null : reader.GetString(reader.GetOrdinal("CustomType"));

//                        string tableKey = schema + "." + tableName;
//                        if (!tableDict.TryGetValue(tableKey, out var table))
//                        {
//                            table = new TableInfo { Schema = schema, Name = tableName };
//                            tableDict[tableKey] = table;
//                        }

//                        table.Columns.Add(new ColumnInfo
//                        {
//                            Name = columnName,
//                            Type = string.IsNullOrEmpty(customType) ? dataType : customType,
//                            Pk = isPk,
//                            Fk = fkTable != null ? new ForeignKeyInfo
//                            {
//                                Schema = fkSchema,
//                                Table = fkTable,
//                                Column = fkColumn
//                            } : null
//                        });
//                    }
//                }

//                databaseSchema.Tables = new List<TableInfo>(tableDict.Values);
//            }

//            return JsonConvert.SerializeObject(databaseSchema, Formatting.Indented);
//        }
//    }

//    #region JSON Model Classes

//    public class DatabaseSchema
//    {
//        public string Database { get; set; }
//        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
//    }

//    public class TableInfo
//    {
//        public string Name { get; set; }
//        public string Schema { get; set; }
//        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
//    }

//    public class ColumnInfo
//    {
//        public string Name { get; set; }
//        public string Type { get; set; }
//        public bool Pk { get; set; }
//        public ForeignKeyInfo Fk { get; set; } = null;
//    }

//    public class ForeignKeyInfo
//    {
//        public string Schema { get; set; }
//        public string Table { get; set; }
//        public string Column { get; set; }
//    }

//    #endregion
//}


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
