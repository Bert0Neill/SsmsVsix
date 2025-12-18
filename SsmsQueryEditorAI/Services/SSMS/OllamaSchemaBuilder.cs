using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Newtonsoft.Json;
    using SSMS_AI_Buddy.Models;
    using System.Collections.Generic;
    using System.Linq;
namespace SSMS_AI_Buddy.Services.SSMS
{
        public static class OllamaSchemaBuilder
        {
            public static string Build(
                string database,
                CachedSchema schema,
                HashSet<string> tablesToInclude)
            {
                var result = new
                {
                    database = database,
                    tables = schema.Tables
                        .Where(t => tablesToInclude.Contains(t.Name))
                        .Select(t => new
                        {
                            name = t.Name,
                            alias = t.Name.Substring(0, 1).ToLower(),
                            columns = schema.Columns
                                .Where(c => c.TableName == t.Name)
                                .Select(c => new
                                {
                                    name = c.Name,
                                    type = c.DataType,
                                    pk = schema.PrimaryKeys.Any(pk =>
                                        pk.Table == t.Name && pk.Column == c.Name),
                                    fk = schema.ForeignKeys
                                        .Where(fk => fk.Table == t.Name && fk.Column == c.Name)
                                        .Select(fk => new
                                        {
                                            table = fk.ReferencedTable,
                                            column = fk.ReferencedColumn
                                        })
                                        .FirstOrDefault()
                                })
                        })
                };

                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
        }
    }

