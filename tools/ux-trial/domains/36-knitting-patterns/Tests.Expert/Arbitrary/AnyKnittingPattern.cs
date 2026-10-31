// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KnittingPattern --force` overwrites it. Reworked by hand: sections follow the cast-on. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

using KnittingPatterns.Tests.Expert.Recipes;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="KnittingPattern" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKnittingPattern : IAny<KnittingPattern> {

    private readonly IAny<string>                        _title;
    private readonly IAny<Gauge>                         _gauge;
    private readonly IAny<int>                           _castOnCount;
    private readonly Func<int, IAny<IReadOnlyList<PatternSection>>> _sectionsFor;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKnittingPattern()
        : this(title:       AnyValidTitle(),
               gauge:       AnyValidGauge(),
               castOnCount: AnyValidCastOnCount(),
               sectionsFor: AnyValidSectionsFor) { }

    // The domain bounds no title length; a printed pattern title reads like "Seaside cardigan".
    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank().WithLengthBetween(3, 80);
    }

    private static AnyGauge AnyValidGauge() {
        return new AnyGauge();
    }

    private static AnyInt32 AnyValidCastOnCount() {
        return Any.Int32().Between(1, 200);
    }

    // The sections depend on the cast-on: by default one section of plain rows that works the
    // cast-on stitches, so a drawn pattern always expands. A test about the rows pins both.
    private static IAny<IReadOnlyList<PatternSection>> AnyValidSectionsFor(int castOnCount) {
        return Any.Combine(
            new AnyPatternSection().Covering(1, 1),
            Any.Int32().Between(1, 12),
            (named, rowCount) => (IReadOnlyList<PatternSection>)[
                new PatternSection(named.Name, Enumerable.Range(1, rowCount).Select(n => Rows.Plain(n, castOnCount).Generate()).ToArray())
            ]);
    }

    private AnyKnittingPattern(IAny<string>                        title,
                               IAny<Gauge>                         gauge,
                               IAny<int>                           castOnCount,
                               Func<int, IAny<IReadOnlyList<PatternSection>>> sectionsFor) {
        _title       = title;
        _gauge       = gauge;
        _castOnCount = castOnCount;
        _sectionsFor = sectionsFor;
    }

    /// <summary>Pins <c>title</c> to a fixed value.</summary>
    public AnyKnittingPattern WithTitle(string value) {
        return new AnyKnittingPattern(new FixedValue<string>(value), _gauge, _castOnCount, _sectionsFor);
    }

    /// <summary>Draws <c>title</c> from <c>AnyValidTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithTitle(Func<AnyString, AnyString> configure) {
        return new AnyKnittingPattern(configure(AnyValidTitle()), _gauge, _castOnCount, _sectionsFor);
    }

    /// <summary>Pins <c>gauge</c> to a fixed value.</summary>
    public AnyKnittingPattern WithGauge(Gauge value) {
        return new AnyKnittingPattern(_title, new FixedValue<Gauge>(value), _castOnCount, _sectionsFor);
    }

    /// <summary>Draws <c>gauge</c> from <c>AnyValidGauge()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithGauge(Func<AnyGauge, AnyGauge> configure) {
        return new AnyKnittingPattern(_title, configure(AnyValidGauge()), _castOnCount, _sectionsFor);
    }

    /// <summary>Pins <c>castOnCount</c> to a fixed value.</summary>
    public AnyKnittingPattern WithCastOnCount(int value) {
        return new AnyKnittingPattern(_title, _gauge, new FixedValue<int>(value), _sectionsFor);
    }

    /// <summary>Draws <c>castOnCount</c> from <c>AnyValidCastOnCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithCastOnCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyKnittingPattern(_title, _gauge, configure(AnyValidCastOnCount()), _sectionsFor);
    }

    /// <summary>Pins <c>sections</c>; pin the cast-on they work with too.</summary>
    public AnyKnittingPattern WithSections(params PatternSection[] value) {
        return new AnyKnittingPattern(_title, _gauge, _castOnCount, _ => new FixedValue<IReadOnlyList<PatternSection>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="KnittingPattern" />.</summary>
    public KnittingPattern Generate() {
        int castOnCount = _castOnCount.Generate();

        return new KnittingPattern(_title.Generate(),
                                   _gauge.Generate(),
                                   castOnCount,
                                   _sectionsFor(castOnCount).Generate());
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
