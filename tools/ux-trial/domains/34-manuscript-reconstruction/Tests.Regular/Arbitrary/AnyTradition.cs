// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tradition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tradition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTradition : IAny<Tradition> {

    private readonly IAny<string> _workTitle;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTradition()
        : this(workTitle: AnyValidWorkTitle()) { }

    private static AnyString AnyValidWorkTitle() {
        return Any.String().NotBlank();
    }

    private AnyTradition(IAny<string> workTitle) {
        _workTitle = workTitle;
    }

    /// <summary>Pins <c>workTitle</c> to a fixed value.</summary>
    public AnyTradition WithWorkTitle(string value) {
        return new AnyTradition(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>workTitle</c> from <c>AnyValidWorkTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTradition WithWorkTitle(Func<AnyString, AnyString> configure) {
        return new AnyTradition(configure(AnyValidWorkTitle()));
    }

    /// <summary>Produces one arbitrary <see cref="Tradition" />.</summary>
    public Tradition Generate() {
        return new Tradition(_workTitle.Generate());
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
