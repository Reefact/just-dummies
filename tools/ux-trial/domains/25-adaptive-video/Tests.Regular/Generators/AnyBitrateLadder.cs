// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BitrateLadder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BitrateLadder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBitrateLadder : IAny<BitrateLadder> {

    private readonly IAny<IEnumerable<EncodingVariant>> _variants;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrateLadder()
        : this(variants: AnyValidVariants()) { }

    private static IAny<IEnumerable<EncodingVariant>> AnyValidVariants() {
        // Unique ids, distinct bitrates and a resolution that never decreases as the bitrate rises:
        // rules between the variants, so they are written once in LadderVariants.
        return new LadderVariants(bitratesKbps: null);
    }

    private AnyBitrateLadder(IAny<IEnumerable<EncodingVariant>> variants) {
        _variants = variants;
    }

    /// <summary>Pins <c>variants</c> to a fixed value.</summary>
    public AnyBitrateLadder WithVariants(IEnumerable<EncodingVariant> value) {
        return new AnyBitrateLadder(new FixedValue<IEnumerable<EncodingVariant>>(value));
    }


    /// <summary>Produces one arbitrary <see cref="BitrateLadder" />.</summary>
    public BitrateLadder Generate() {
        return new BitrateLadder(_variants.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
