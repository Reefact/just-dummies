// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StratigraphicLayer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StratigraphicLayer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStratigraphicLayer : IAny<StratigraphicLayer> {

    private readonly IAny<string> _code;
    private readonly IAny<string> _description;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStratigraphicLayer()
        : this(code:        AnyValidCode(),
               description: AnyValidDescription()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}");
    }

    private static AnyString AnyValidDescription() {
        return Any.String().NotBlank().WithMaxLength(500);
    }

    private AnyStratigraphicLayer(IAny<string> code,
                                  IAny<string> description) {
        _code        = code;
        _description = description;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyStratigraphicLayer WithCode(string value) {
        return new AnyStratigraphicLayer(new FixedValue<string>(value), _description);
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStratigraphicLayer WithCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyStratigraphicLayer(configure(AnyValidCode()), _description);
    }

    /// <summary>Pins <c>description</c> to a fixed value.</summary>
    public AnyStratigraphicLayer WithDescription(string value) {
        return new AnyStratigraphicLayer(_code, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>description</c> from <c>AnyValidDescription()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStratigraphicLayer WithDescription(Func<AnyString, AnyString> configure) {
        return new AnyStratigraphicLayer(_code, configure(AnyValidDescription()));
    }

    /// <summary>Produces one arbitrary <see cref="StratigraphicLayer" />.</summary>
    public StratigraphicLayer Generate() {
        return new StratigraphicLayer(_code.Generate(),
                                      _description.Generate());
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
