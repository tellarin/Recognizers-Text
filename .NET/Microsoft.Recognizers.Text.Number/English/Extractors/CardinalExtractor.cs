using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Definitions.English;
using Microsoft.Recognizers.Text.InternalCache;

namespace Microsoft.Recognizers.Text.Number.English
{
    public class CardinalExtractor : BaseNumberExtractor
    {
        private static readonly ConcurrentDictionary<string, CardinalExtractor> Instances =
            new ConcurrentDictionary<string, CardinalExtractor>();

        private static readonly ResultsCache<ExtractResult> ResultsCache = new ResultsCache<ExtractResult>();

        private readonly string placeholder;

        private CardinalExtractor(NumberOptions options, string placeholder)
            : base(options)
        {

            this.placeholder = placeholder;

            var builder = ImmutableDictionary.CreateBuilder<Regex, TypeTag>();

            // Add Integer Regexes
            var intExtract = IntegerExtractor.GetInstance(options, placeholder);
            builder.AddRange(intExtract.Regexes);

            // Add Double Regexes
            var douExtract = DoubleExtractor.GetInstance(options, placeholder);
            builder.AddRange(douExtract.Regexes);

            Regexes = builder.ToImmutable();
        }

        internal sealed override ImmutableDictionary<Regex, TypeTag> Regexes { get; }

        protected sealed override string ExtractType { get; } = Constants.SYS_NUM_CARDINAL; // "Cardinal";

        public static CardinalExtractor GetInstance(NumberOptions options = NumberOptions.None,
                                                    string placeholder = NumbersDefinitions.PlaceHolderDefault)
        {

            if (!Instances.ContainsKey(placeholder))
            {
                var instance = new CardinalExtractor(options, placeholder);
                Instances.TryAdd(placeholder, instance);
            }

            return Instances[placeholder];
        }

        public override List<ExtractResult> Extract(string source)
        {
            var key = Options + "_" + placeholder + "_" + source;

            return ResultsCache.GetOrCreate(key, () => base.Extract(source));
        }
    }
}