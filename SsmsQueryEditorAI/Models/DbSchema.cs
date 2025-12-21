using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Models
{
    public class TableInfo
    {
        public string Name { get; set; }
        public string Schema { get; set; }   // New: schema name
    }

    public class ColumnInfo
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Schema { get; set; }   // New: schema name
    }

    public class PrimaryKeyInfo
    {
        public string Schema { get; set; }   // New: schema name
        public string Table { get; set; }    // Table name
        public string Column { get; set; }   // Column name
    }

    public class ForeignKeyInfo
    {
        public string Schema { get; set; }           // e.g., "SalesLT"
        public string Table { get; set; }            // e.g., "Product"
        public string Column { get; set; }
        public string ReferencedSchema { get; set; } // e.g., "SalesLT"
        public string ReferencedTable { get; set; }  // e.g., "ProductCategory"
        public string ReferencedColumn { get; set; }
    }

    public class IndexInfo
    {
        public string Schema { get; set; }       // New: schema name
        public string Table { get; set; }        // Table name
        public string Name { get; set; }         // Index name
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public List<string> IncludedColumns { get; set; } = new List<string>();
    }


    public class CachedSchema
    {
        public List<TableInfo> Tables { get; set; }
        public List<ColumnInfo> Columns { get; set; }
        public List<PrimaryKeyInfo> PrimaryKeys { get; set; }
        public List<ForeignKeyInfo> ForeignKeys { get; set; }
        public List<IndexInfo> Indexes { get; set; }
    }
}


