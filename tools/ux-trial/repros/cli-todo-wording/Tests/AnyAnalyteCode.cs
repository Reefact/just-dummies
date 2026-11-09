// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AnalyteCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TodoWording.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AnalyteCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAnalyteCode : IAny<AnalyteCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAnalyteCode()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // TODO(any): 'string value' may be guarded by something the scaffolder could not read (§9).
        //   This is the scaffolder's best generator for the type; verify it honours the real invariant,
        //   or replace it, then delete the line below.

        return Any.StringMatching(@"^\d{1,5}-\d$");
    }

    private AnyAnalyteCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyAnalyteCode WithValue(string value) {
        return new AnyAnalyteCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAnalyteCode WithValue(Func<AnyString, AnyString> configure) {
        return new AnyAnalyteCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="AnalyteCode" />.</summary>
    public AnalyteCode Generate() {
        return new AnalyteCode(_value.Generate());
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
