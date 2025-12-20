using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
   using System.Collections.Generic;
namespace SSMS_AI_Buddy.Models
{
 

    internal sealed class SqlCompletionContext
    {
        public IReadOnlyList<string> Items { get; }

        public SqlCompletionContext(IEnumerable<string> items)
        {
            Items = new List<string>(items);
        }
    }

}
