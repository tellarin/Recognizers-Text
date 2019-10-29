﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Definitions.English;

namespace Microsoft.Recognizers.Text.Number.English
{
    public class OrdinalExtractor : BaseNumberExtractor
    {

        private const RegexOptions RegexFlags = RegexOptions.Singleline | RegexOptions.ExplicitCapture;

        private static readonly ConcurrentDictionary<string, OrdinalExtractor> Instances =
            new ConcurrentDictionary<string, OrdinalExtractor>();

        private static readonly Dictionary<string, List<ExtractResult>> ResultCache = new Dictionary<string, List<ExtractResult>>();

        private OrdinalExtractor(NumberOptions options)
            : base(options)
        {
            AmbiguousFractionConnectorsRegex = new Regex(NumbersDefinitions.AmbiguousFractionConnectorsRegex, RegexFlags);

            RelativeReferenceRegex = new Regex(NumbersDefinitions.RelativeOrdinalRegex, RegexFlags);

            var regexes = new Dictionary<Regex, TypeTag>
            {
                {
                    new Regex(NumbersDefinitions.OrdinalSuffixRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.NUMBER_SUFFIX)
                },
                {
                    new Regex(NumbersDefinitions.OrdinalNumericRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.NUMBER_SUFFIX)
                },
                {
                    new Regex(NumbersDefinitions.OrdinalEnglishRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.ENGLISH)
                },
                {
                    new Regex(NumbersDefinitions.OrdinalRoundNumberRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.ENGLISH)
                },
            };

            Regexes = regexes.ToImmutableDictionary();
        }

        internal sealed override ImmutableDictionary<Regex, TypeTag> Regexes { get; }

        protected sealed override string ExtractType { get; } = Constants.SYS_NUM_ORDINAL; // "Ordinal";

        protected sealed override Regex AmbiguousFractionConnectorsRegex { get; }

        protected sealed override Regex RelativeReferenceRegex { get; }

        public static OrdinalExtractor GetInstance(NumberOptions options = NumberOptions.None)
        {
            var cacheKey = options.ToString();
            if (!Instances.ContainsKey(cacheKey))
            {
                var instance = new OrdinalExtractor(options);
                Instances.TryAdd(cacheKey, instance);
            }

            return Instances[cacheKey];
        }

        public override List<ExtractResult> Extract(string source)
        {
            var key = Options + "_" + source;

            List<ExtractResult> val = new List<ExtractResult>();

            if (!ResultCache.TryGetValue(key, out val))
            {
                val = base.Extract(source);
                ResultCache[key] = val;
            }

            return new List<ExtractResult>(val);
        }

    }
}