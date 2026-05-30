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
            if (string.IsNullOrWhiteSpace(key))
                return null;

            CachedSchema schema;

            if (_cache.TryGetValue(key, out schema))
                return schema;

            schema = loader();
            _cache[key] = schema;

            return schema;
        }
    }
}
