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
    }

    public class ColumnInfo
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
    }

    public class PrimaryKeyInfo
    {
        public string Table { get; set; }
        public string Column { get; set; }
    }

    public class ForeignKeyInfo
    {
        public string Table { get; set; }
        public string Column { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
    }

    public class IndexInfo
    {
        public string Table { get; set; }
        public string Name { get; set; }
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public List<string> Columns { get; set; }
        public List<string> IncludedColumns { get; set; }
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


