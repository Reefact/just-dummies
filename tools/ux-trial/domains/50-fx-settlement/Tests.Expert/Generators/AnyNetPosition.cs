// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NetPosition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using FxSettlement.Domain;
using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>
///     A generator of arbitrary <see cref="NetPosition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNetPosition : IAny<NetPosition> {

    private IAny<Lei>   _counterparty;
    private IAny<Money> _net;

    private AnyLei   _counterpartyRecipe;
    private AnyMoney _netRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNetPosition()
        : this(counterparty: AnyValidCounterparty(),
               net:          AnyValidNet()) { }

    private static AnyLei AnyValidCounterparty() {
        return new AnyLei();
    }

    private static AnyMoney AnyValidNet() {
        return new AnyMoney();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNetPosition(AnyLei   counterparty,
                           AnyMoney net) {
        _counterparty       = counterparty;
        _counterpartyRecipe = counterparty;
        _net                = net;
        _netRecipe          = net;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNetPosition(AnyNetPosition from) {
        _counterparty       = from._counterparty;
        _counterpartyRecipe = from._counterpartyRecipe;
        _net                = from._net;
        _netRecipe          = from._netRecipe;
    }

    /// <summary>Pins <c>counterparty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNetPosition WithCounterparty(Lei value) {
        AnyNetPosition copy = new AnyNetPosition(this);

        copy._counterparty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>counterparty</c> is drawn from — <c>AnyValidCounterparty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLei, AnyLei&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNetPosition WithCounterparty(Func<AnyLei, AnyLei> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNetPosition copy = new AnyNetPosition(this);

        copy._counterpartyRecipe = configure(_counterpartyRecipe);
        copy._counterparty       = copy._counterpartyRecipe;

        return copy;
    }

    /// <summary>Pins <c>net</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNetPosition WithNet(Money value) {
        AnyNetPosition copy = new AnyNetPosition(this);

        copy._net = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>net</c> is drawn from — <c>AnyValidNet()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNetPosition WithNet(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNetPosition copy = new AnyNetPosition(this);

        copy._netRecipe = configure(_netRecipe);
        copy._net       = copy._netRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NetPosition" />.</summary>
    public NetPosition Generate() {
        return new NetPosition(_counterparty.Generate(),
                               _net.Generate());
    }

}
