using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class OllamaSchemaBuilder
    {

        //public static string BuildSlimSchemaRelationship(string database, CachedSchema schema, HashSet<string> tablesToInclude)
        //{
        //    var relations = schema.ForeignKeys
        //        .Where(fk =>
        //            tablesToInclude.Contains($"{fk.Schema}.{fk.Table}") &&
        //            tablesToInclude.Contains($"{fk.ReferencedSchema}.{fk.ReferencedTable}"))
        //        .Select(fk => new
        //        {
        //            from = $"{fk.Schema}.{fk.Table}.{fk.Column}",
        //            to = $"{fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}"
        //        })
        //        .Distinct()
        //        .ToList();

        //    return JsonConvert.SerializeObject(relations, Formatting.Indented);
        //}

        public static string BuildSlimSchemaRelationship(string database, CachedSchema schema, HashSet<string> tablesToInclude)
        {
            if (schema?.ForeignKeys == null || tablesToInclude == null || tablesToInclude.Count == 0)
                return "[]";

            var relations = schema.ForeignKeys
                .Where(fk =>
                {
                    var fkTable = $"{fk.Schema}.{fk.Table}";
                    var pkTable = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";

                    return tablesToInclude.Contains(fkTable) &&
                           tablesToInclude.Contains(pkTable);
                })
                .Select(fk => new
                {
                    fkTable = $"{fk.Schema}.{fk.Table}",
                    fkColumn = fk.Column,
                    pkTable = $"{fk.ReferencedSchema}.{fk.ReferencedTable}",
                    pkColumn = fk.ReferencedColumn
                    //type = "many-to-one"                    
        })
                .Distinct()
                .ToList();

            return JsonConvert.SerializeObject(relations, Formatting.None);
        }

        public static string Build(string database, CachedSchema schema, HashSet<string> tablesToInclude)
        {
            var result = new
            {
                database = database,
                tables = schema.Tables
                    .Where(t => tablesToInclude.Contains($"{t.Schema}.{t.Name}"))
                    .Select(t => new
                    {
                        name = t.Name,
                        schema = t.Schema,
                        columns = schema.Columns
                            .Where(c => c.TableName == t.Name && c.Schema == t.Schema)
                            .Select(c => new
                            {
                                name = c.Name,
                                type = c.DataType,
                                pk = schema.PrimaryKeys.Any(pk =>
                                    pk.Table == t.Name &&
                                    pk.Schema == t.Schema &&
                                    pk.Column == c.Name),
                                fk = schema.ForeignKeys
                                    .Where(fk =>
                                        fk.Table == t.Name &&
                                        fk.Schema == t.Schema &&
                                        fk.Column == c.Name)
                                    .Select(fk => new
                                    {
                                        schema = fk.ReferencedSchema,
                                        table = fk.ReferencedTable,
                                        column = fk.ReferencedColumn
                                    })
                                    .FirstOrDefault() // null if no FK
                            })
                            .ToList() // ensure columns is a JSON array
                    })
                    .ToList() // ensure tables is a JSON array
            };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}



//using System.Collections.Generic;
//using System.Linq;
//using Newtonsoft.Json;
//using SSMS_AI_Buddy.Models;

//namespace SSMS_AI_Buddy.Services.SSMS
//{
//    public static class OllamaSchemaBuilder
//    {
//        public static string Build(string database, CachedSchema schema, HashSet<string> tablesToInclude)
//        {
//            var result = new
//            {
//                database = database,
//                tables = schema.Tables
//                    .Where(t => tablesToInclude.Contains($"{t.Schema}.{t.Name}"))
//                    .Select(t => new
//                    {
//                        name = t.Name,
//                        schema = t.Schema,
//                        columns = schema.Columns
//                            .Where(c => c.TableName == t.Name && c.Schema == t.Schema)
//                            .Select(c => new
//                            {
//                                name = c.Name,
//                                type = c.DataType,
//                                pk = schema.PrimaryKeys.Any(pk =>
//                                    pk.Table == t.Name && pk.Schema == t.Schema && pk.Column == c.Name),
//                                fk = schema.ForeignKeys
//                                    .Where(fk =>
//                                        fk.Table == t.Name &&
//                                        fk.Schema == t.Schema &&
//                                        fk.Column == c.Name)
//                                    .Select(fk => new
//                                    {
//                                        schema = fk.ReferencedSchema,
//                                        table = fk.ReferencedTable,
//                                        column = fk.ReferencedColumn
//                                    })
//                                    .FirstOrDefault()
//                            })
//                    })
//            };

//            return JsonConvert.SerializeObject(result, Formatting.Indented);
//        }
//    }
//}
