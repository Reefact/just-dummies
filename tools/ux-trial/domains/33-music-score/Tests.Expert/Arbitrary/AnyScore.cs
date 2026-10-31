// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Score --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Score" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScore : IAny<Score> {

    private readonly IAny<string>               _title;
    private readonly IAny<Tempo>                _tempo;
    private readonly IAny<IReadOnlyList<Staff>> _staves;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScore()
        : this(title:  AnyValidTitle(),
               tempo:  AnyValidTempo(),
               staves: AnyValidStaves()) { }

    private static AnyString AnyValidTitle() {
        // The domain sets no cap; a title as printed on a cover page is a line, not a chapter.
        return Any.String().NotBlank().WithLengthBetween(1, 120);
    }

    private static AnyTempo AnyValidTempo() {
        return new AnyTempo();
    }

    private static IAny<IReadOnlyList<Staff>> AnyValidStaves() {
        // Staves of one score share their measure count, meters and repeat barlines; one arbitrary
        // staff honours that by construction. A test that needs several writes them over one skeleton.
        return new AnyStaff().As(staff => (IReadOnlyList<Staff>)[staff]);
    }

    private AnyScore(IAny<string>               title,
                     IAny<Tempo>                tempo,
                     IAny<IReadOnlyList<Staff>> staves) {
        _title  = title;
        _tempo  = tempo;
        _staves = staves;
    }

    /// <summary>Pins <c>title</c> to a fixed value.</summary>
    public AnyScore WithTitle(string value) {
        return new AnyScore(new FixedValue<string>(value), _tempo, _staves);
    }

    /// <summary>Draws <c>title</c> from <c>AnyValidTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScore WithTitle(Func<AnyString, AnyString> configure) {
        return new AnyScore(configure(AnyValidTitle()), _tempo, _staves);
    }

    /// <summary>Pins <c>tempo</c> to a fixed value.</summary>
    public AnyScore WithTempo(Tempo value) {
        return new AnyScore(_title, new FixedValue<Tempo>(value), _staves);
    }

    /// <summary>Draws <c>tempo</c> from <c>AnyValidTempo()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScore WithTempo(Func<AnyTempo, AnyTempo> configure) {
        return new AnyScore(_title, configure(AnyValidTempo()), _staves);
    }

    /// <summary>Pins <c>staves</c> to a fixed value.</summary>
    public AnyScore WithStaves(IReadOnlyList<Staff> value) {
        return new AnyScore(_title, _tempo, new FixedValue<IReadOnlyList<Staff>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Score" />.</summary>
    public Score Generate() {
        return new Score(_title.Generate(),
                         _tempo.Generate(),
                         _staves.Generate());
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
