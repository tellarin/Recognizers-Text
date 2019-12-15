// ReSharper disable StaticMemberInGenericType

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Recognizers.Text.InternalCache
{
    public class ResultsCache<TItem>
        where TItem : ICloneableType<TItem>
    {

        private const long CacheSize = 20000;

        private const double CompactionPercentage = 0.4;

        private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);

        private static readonly MemoryCacheOptions CacheOptions = new MemoryCacheOptions
        {
            SizeLimit = CacheSize,
            CompactionPercentage = CompactionPercentage,
            ExpirationScanFrequency = TimeSpan.FromHours(24),
        };

        private readonly IMemoryCache resultsCache = new MemoryCache(CacheOptions);

        public List<TItem> GetOrCreate(object key, Func<List<TItem>> createItem)
        {

            if (!resultsCache.TryGetValue(key, out List<TItem> results))
            {
                results = createItem();

                resultsCache.Set(key, results, CacheEntryOptions);
            }

            return results.ConvertAll(e => e.Clone());
        }

    }
}
