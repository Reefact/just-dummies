// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PowerInjection --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PowerInjection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPowerInjection : IAny<PowerInjection> {

    private IAny<double> _activeMw;
    private IAny<double> _reactiveMvar;

    private AnyDouble _activeMwRecipe;
    private AnyDouble _reactiveMvarRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPowerInjection()
        : this(activeMw:     AnyValidActiveMw(),
               reactiveMvar: AnyValidReactiveMvar()) { }

    private static AnyDouble AnyValidActiveMw() {
        return Any.Double().Between(-10000d, 10000d);
    }

    private static AnyDouble AnyValidReactiveMvar() {
        return Any.Double().Between(-10000d, 10000d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPowerInjection(AnyDouble activeMw,
                              AnyDouble reactiveMvar) {
        _activeMw           = activeMw;
        _activeMwRecipe     = activeMw;
        _reactiveMvar       = reactiveMvar;
        _reactiveMvarRecipe = reactiveMvar;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPowerInjection(AnyPowerInjection from) {
        _activeMw           = from._activeMw;
        _activeMwRecipe     = from._activeMwRecipe;
        _reactiveMvar       = from._reactiveMvar;
        _reactiveMvarRecipe = from._reactiveMvarRecipe;
    }

    /// <summary>Pins <c>activeMw</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPowerInjection WithActiveMw(double value) {
        AnyPowerInjection copy = new AnyPowerInjection(this);

        copy._activeMw = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>activeMw</c> is drawn from — <c>AnyValidActiveMw()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPowerInjection WithActiveMw(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPowerInjection copy = new AnyPowerInjection(this);

        copy._activeMwRecipe = configure(_activeMwRecipe);
        copy._activeMw       = copy._activeMwRecipe;

        return copy;
    }

    /// <summary>Pins <c>reactiveMvar</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPowerInjection WithReactiveMvar(double value) {
        AnyPowerInjection copy = new AnyPowerInjection(this);

        copy._reactiveMvar = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reactiveMvar</c> is drawn from — <c>AnyValidReactiveMvar()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPowerInjection WithReactiveMvar(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPowerInjection copy = new AnyPowerInjection(this);

        copy._reactiveMvarRecipe = configure(_reactiveMvarRecipe);
        copy._reactiveMvar       = copy._reactiveMvarRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PowerInjection" />.</summary>
    public PowerInjection Generate() {
        return new PowerInjection(_activeMw.Generate(),
                                  _reactiveMvar.Generate());
    }

}
