// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KnittingPattern --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

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
    private readonly IAny<IReadOnlyList<PatternSection>> _sections;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKnittingPattern()
        : this(title:       AnyValidTitle(),
               gauge:       AnyValidGauge(),
               castOnCount: AnyValidCastOnCount(),
               sections:    AnyValidSections()) { }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank();
    }

    private static AnyGauge AnyValidGauge() {
        return new AnyGauge();
    }

    private static AnyInt32 AnyValidCastOnCount() {
        return Any.Int32().Between(1, KnittingPattern.MaxCastOn);
    }

    private static IAny<IReadOnlyList<PatternSection>> AnyValidSections() {
        // Sections are contiguous from row 1 and uniquely named: one section starting at row 1 always is.
        // Its rows are not tied to the cast-on, so a test that expands the pattern pins its sections.
        return new AnyPatternSection().StartingAtRow(1).As(section => (IReadOnlyList<PatternSection>)[section]);
    }

    private AnyKnittingPattern(IAny<string>                        title,
                               IAny<Gauge>                         gauge,
                               IAny<int>                           castOnCount,
                               IAny<IReadOnlyList<PatternSection>> sections) {
        _title       = title;
        _gauge       = gauge;
        _castOnCount = castOnCount;
        _sections    = sections;
    }

    /// <summary>Pins <c>title</c> to a fixed value.</summary>
    public AnyKnittingPattern WithTitle(string value) {
        return new AnyKnittingPattern(new FixedValue<string>(value), _gauge, _castOnCount, _sections);
    }

    /// <summary>Draws <c>title</c> from <c>AnyValidTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithTitle(Func<AnyString, AnyString> configure) {
        return new AnyKnittingPattern(configure(AnyValidTitle()), _gauge, _castOnCount, _sections);
    }

    /// <summary>Pins <c>gauge</c> to a fixed value.</summary>
    public AnyKnittingPattern WithGauge(Gauge value) {
        return new AnyKnittingPattern(_title, new FixedValue<Gauge>(value), _castOnCount, _sections);
    }

    /// <summary>Draws <c>gauge</c> from <c>AnyValidGauge()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithGauge(Func<AnyGauge, AnyGauge> configure) {
        return new AnyKnittingPattern(_title, configure(AnyValidGauge()), _castOnCount, _sections);
    }

    /// <summary>Pins <c>castOnCount</c> to a fixed value.</summary>
    public AnyKnittingPattern WithCastOnCount(int value) {
        return new AnyKnittingPattern(_title, _gauge, new FixedValue<int>(value), _sections);
    }

    /// <summary>Draws <c>castOnCount</c> from <c>AnyValidCastOnCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyKnittingPattern WithCastOnCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyKnittingPattern(_title, _gauge, configure(AnyValidCastOnCount()), _sections);
    }

    /// <summary>Pins <c>sections</c> to a fixed value.</summary>
    public AnyKnittingPattern WithSections(IReadOnlyList<PatternSection> value) {
        return new AnyKnittingPattern(_title, _gauge, _castOnCount, new FixedValue<IReadOnlyList<PatternSection>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="KnittingPattern" />.</summary>
    public KnittingPattern Generate() {
        return new KnittingPattern(_title.Generate(),
                                   _gauge.Generate(),
                                   _castOnCount.Generate(),
                                   _sections.Generate());
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
