// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ipv4Address --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ipv4Address" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIpv4Address : IAny<Ipv4Address> {

    private IAny<uint> _value;

    private AnyUInt32 _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIpv4Address()
        : this(value: AnyValidValue()) { }

    private static AnyUInt32 AnyValidValue() {
        return Any.UInt32();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIpv4Address(AnyUInt32 value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIpv4Address(AnyIpv4Address from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIpv4Address WithValue(uint value) {
        AnyIpv4Address copy = new AnyIpv4Address(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt32, AnyUInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIpv4Address WithValue(Func<AnyUInt32, AnyUInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIpv4Address copy = new AnyIpv4Address(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Ipv4Address" />.</summary>
    public Ipv4Address Generate() {
        return new Ipv4Address(_value.Generate());
    }

}
