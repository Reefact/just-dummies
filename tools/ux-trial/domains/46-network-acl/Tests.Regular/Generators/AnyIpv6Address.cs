// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ipv6Address --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ipv6Address" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIpv6Address : IAny<Ipv6Address> {

    private IAny<UInt128> _value;

    private AnyUInt128 _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIpv6Address()
        : this(value: AnyValidValue()) { }

    private static AnyUInt128 AnyValidValue() {
        return Any.UInt128();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIpv6Address(AnyUInt128 value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIpv6Address(AnyIpv6Address from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIpv6Address WithValue(UInt128 value) {
        AnyIpv6Address copy = new AnyIpv6Address(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt128, AnyUInt128&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIpv6Address WithValue(Func<AnyUInt128, AnyUInt128> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIpv6Address copy = new AnyIpv6Address(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Ipv6Address" />.</summary>
    public Ipv6Address Generate() {
        return new Ipv6Address(_value.Generate());
    }

}
