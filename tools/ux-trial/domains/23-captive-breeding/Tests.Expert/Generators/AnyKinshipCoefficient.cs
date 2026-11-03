// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate KinshipCoefficient --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using CaptiveBreeding.Domain;
using JustDummies;

namespace CaptiveBreeding.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="KinshipCoefficient" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyKinshipCoefficient : IAny<KinshipCoefficient> {

    private IAny<double> _value;

    private AnyDouble _valueRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyKinshipCoefficient()
        : this(value: AnyValidValue()) { }

    private static AnyDouble AnyValidValue() {
        return Any.Double().Between(0d, 1d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyKinshipCoefficient(AnyDouble value) {
        _value       = value;
        _valueRecipe = value;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyKinshipCoefficient(AnyKinshipCoefficient from) {
        _value       = from._value;
        _valueRecipe = from._valueRecipe;
    }

    /// <summary>Pins <c>value</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyKinshipCoefficient WithValue(double value) {
        AnyKinshipCoefficient copy = new AnyKinshipCoefficient(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>value</c> is drawn from — <c>AnyValidValue()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyKinshipCoefficient WithValue(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyKinshipCoefficient copy = new AnyKinshipCoefficient(this);

        copy._valueRecipe = configure(_valueRecipe);
        copy._value       = copy._valueRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="KinshipCoefficient" />.</summary>
    public KinshipCoefficient Generate() {
        return new KinshipCoefficient(_value.Generate());
    }

}
