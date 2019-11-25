// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Recognizers.Text.InternalCache
{
    public class ResultsCache<TItem>
        where TItem : ICloneableType<TItem>
    {

        private const int CacheSize = 10000; // @HERE

        private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);

        private static readonly MemoryCacheOptions CacheOptions = new MemoryCacheOptions
        {
            SizeLimit = CacheSize,
            CompactionPercentage = 0.1,
            ExpirationScanFrequency = TimeSpan.FromHours(24),
        };

        // private readonly ConcurrentDictionary<string, List<TItem>> resultsCache = new ConcurrentDictionary<string, List<TItem>>();

        private readonly IMemoryCache resultsCache = new MemoryCache(CacheOptions);

        public List<TItem> GetOrCreate(string key, Func<List<TItem>> createItem)
        {

            if (!resultsCache.TryGetValue(key, out List<TItem> results))
            {
                results = createItem();

                // resultsCache[key] = results;

                resultsCache.Set(key, results, CacheEntryOptions);
            }

            return results.ConvertAll(e => e.Clone());
        }

    }
}
