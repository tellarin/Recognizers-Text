using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Recognizers.Text.InternalCache
{
    public class ResultsCache<TItem>
        where TItem : ICloneableType<TItem>
    {

        private readonly ConcurrentDictionary<string, List<TItem>> resultsCache = new ConcurrentDictionary<string, List<TItem>>();

        public List<TItem> GetOrCreate(string key, Func<List<TItem>> createItem)
        {
            if (!resultsCache.TryGetValue(key, out List<TItem> results))
            {
                results = createItem();
                resultsCache[key] = results;
            }

            return results.ConvertAll(e => e.Clone()); // @HERE
        }
    }
}
