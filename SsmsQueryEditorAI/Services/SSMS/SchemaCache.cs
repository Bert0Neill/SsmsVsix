using SSMS_AI_Buddy.Models;
using System;
using System.Collections.Generic;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class SchemaCache
    {
        private static readonly Dictionary<string, CachedSchema> _cache = new Dictionary<string, CachedSchema>();

        public static CachedSchema GetOrAdd(string key, Func<CachedSchema> loader)
        {
            if (_cache.ContainsKey(key))
                return _cache[key];

            var schema = loader();
            _cache[key] = schema;
            return schema;
        }
    }
}
