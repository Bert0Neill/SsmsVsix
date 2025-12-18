using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Services.SSMS
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
        public static class SqlAnalyzer
        {
            public static HashSet<string> ExtractTables(string sql)
            {
                var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

                var regex = new Regex(@"\b(from|join)\s+([\[\]\w\.]+)",
                    RegexOptions.IgnoreCase);

                foreach (Match m in regex.Matches(sql))
                {
                    var raw = m.Groups[2].Value;
                    var name = raw.Split('.')[raw.Split('.').Length - 1]
                                   .Trim('[', ']');

                    set.Add(name);
                }

                return set;
            }

            public static bool ShouldTrigger(string sql)
            {
                return sql.Length > 10 &&
                       Regex.IsMatch(sql, @"\b(from|join)\b", RegexOptions.IgnoreCase);
            }
        }
    }
