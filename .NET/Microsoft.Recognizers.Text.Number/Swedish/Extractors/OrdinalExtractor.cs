﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Definitions.Swedish;

namespace Microsoft.Recognizers.Text.Number.Swedish
{
    public class OrdinalExtractor : BaseNumberExtractor
    {

        private const RegexOptions RegexFlags = RegexOptions.Singleline | RegexOptions.ExplicitCapture;

        private static readonly ConcurrentDictionary<string, OrdinalExtractor> Instances =
            new ConcurrentDictionary<string, OrdinalExtractor>();

        private OrdinalExtractor()
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
                    new Regex(NumbersDefinitions.OrdinalSwedishRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.SWEDISH)
                },
                {
                    new Regex(NumbersDefinitions.OrdinalRoundNumberRegex, RegexFlags),
                    RegexTagGenerator.GenerateRegexTag(Constants.ORDINAL_PREFIX, Constants.SWEDISH)
                },
            };

            Regexes = regexes.ToImmutableDictionary();
        }

        internal sealed override ImmutableDictionary<Regex, TypeTag> Regexes { get; }

        protected sealed override string ExtractType { get; } = Constants.SYS_NUM_ORDINAL; // "Ordinal";

        protected sealed override Regex AmbiguousFractionConnectorsRegex { get; }

        protected sealed override Regex RelativeReferenceRegex { get; }

        public static OrdinalExtractor GetInstance(string placeholder = "")
        {
            if (!Instances.ContainsKey(placeholder))
            {
                var instance = new OrdinalExtractor();
                Instances.TryAdd(placeholder, instance);
            }

            return Instances[placeholder];
        }
    }
}