// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Batch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Jd030.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Batch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatch : IAny<Batch> {

    private readonly IAny<IReadOnlyList<string>> _tags;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatch()
        : this(tags: AnyValidTags()) { }

    private static AnyList<string> AnyValidTags() {
        return Any.ListOf(Any.String().NonEmpty());
    }

    private AnyBatch(IAny<IReadOnlyList<string>> tags) {
        _tags = tags;
    }

    /// <summary>Pins <c>tags</c> to a fixed value.</summary>
    public AnyBatch WithTags(IReadOnlyList<string> value) {
        return new AnyBatch(new FixedValue<IReadOnlyList<string>>(value));
    }

    /// <summary>Draws <c>tags</c> from <c>AnyValidTags()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBatch WithTags(Func<AnyList<string>, AnyList<string>> configure) {
        return new AnyBatch(configure(AnyValidTags()));
    }

    /// <summary>Produces one arbitrary <see cref="Batch" />.</summary>
    public Batch Generate() {
        return new Batch(_tags.Generate());
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
