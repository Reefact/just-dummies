// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Impedance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using GridLoadFlow.Domain;
using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="Impedance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyImpedance : IAny<Impedance> {

    private IAny<double> _resistancePu;
    private IAny<double> _reactancePu;

    private AnyDouble _resistancePuRecipe;
    private AnyDouble _reactancePuRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyImpedance()
        : this(resistancePu: AnyValidResistancePu(),
               reactancePu:  AnyValidReactancePu()) { }

    private static AnyDouble AnyValidResistancePu() {
        return Any.Double().Between(0d, Impedance.MaximumPu);
    }

    private static AnyDouble AnyValidReactancePu() {
        return Any.Double().Positive().LessThanOrEqualTo(Impedance.MaximumPu);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyImpedance(AnyDouble resistancePu,
                         AnyDouble reactancePu) {
        _resistancePu       = resistancePu;
        _resistancePuRecipe = resistancePu;
        _reactancePu        = reactancePu;
        _reactancePuRecipe  = reactancePu;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyImpedance(AnyImpedance from) {
        _resistancePu       = from._resistancePu;
        _resistancePuRecipe = from._resistancePuRecipe;
        _reactancePu        = from._reactancePu;
        _reactancePuRecipe  = from._reactancePuRecipe;
    }

    /// <summary>Pins <c>resistancePu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyImpedance WithResistancePu(double value) {
        AnyImpedance copy = new AnyImpedance(this);

        copy._resistancePu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>resistancePu</c> is drawn from — <c>AnyValidResistancePu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyImpedance WithResistancePu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyImpedance copy = new AnyImpedance(this);

        copy._resistancePuRecipe = configure(_resistancePuRecipe);
        copy._resistancePu       = copy._resistancePuRecipe;

        return copy;
    }

    /// <summary>Pins <c>reactancePu</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyImpedance WithReactancePu(double value) {
        AnyImpedance copy = new AnyImpedance(this);

        copy._reactancePu = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reactancePu</c> is drawn from — <c>AnyValidReactancePu()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyImpedance WithReactancePu(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyImpedance copy = new AnyImpedance(this);

        copy._reactancePuRecipe = configure(_reactancePuRecipe);
        copy._reactancePu       = copy._reactancePuRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Impedance" />.</summary>
    public Impedance Generate() {
        return new Impedance(_resistancePu.Generate(),
                             _reactancePu.Generate());
    }

}
