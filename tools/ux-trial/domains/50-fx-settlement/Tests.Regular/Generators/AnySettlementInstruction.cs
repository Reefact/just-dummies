// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SettlementInstruction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using FxSettlement.Domain;
using JustDummies;

namespace FxSettlement.Domain.Tests.Regular;

/// <summary>
///     A generator of arbitrary <see cref="SettlementInstruction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySettlementInstruction : IAny<SettlementInstruction> {

    private IAny<Lei>                 _counterparty;
    private IAny<SettlementDirection> _direction;
    private IAny<Money>               _amount;
    private IAny<DateOnly>            _valueDate;

    private AnyLei                       _counterpartyRecipe;
    private AnyEnum<SettlementDirection> _directionRecipe;
    private AnyMoney                     _amountRecipe;
    
    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySettlementInstruction()
        : this(counterparty: AnyValidCounterparty(),
               direction:    AnyValidDirection(),
               amount:       AnyValidAmount(),
               valueDate:    AnyValidValueDate()) { }

    private static AnyLei AnyValidCounterparty() {
        return new AnyLei();
    }

    private static AnyEnum<SettlementDirection> AnyValidDirection() {
        return Any.Enum<SettlementDirection>();
    }

    private static AnyMoney AnyValidAmount() {
        return new AnyMoney().WithAmount(a => a.Positive());
    }

    private static IAny<DateOnly> AnyValidValueDate() {
        return BusinessDays.Any();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySettlementInstruction(AnyLei                       counterparty,
                                     AnyEnum<SettlementDirection> direction,
                                     AnyMoney                     amount,
                                     IAny<DateOnly>               valueDate) {
        _counterparty       = counterparty;
        _counterpartyRecipe = counterparty;
        _direction          = direction;
        _directionRecipe    = direction;
        _amount             = amount;
        _amountRecipe       = amount;
        _valueDate          = valueDate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySettlementInstruction(AnySettlementInstruction from) {
        _counterparty       = from._counterparty;
        _counterpartyRecipe = from._counterpartyRecipe;
        _direction          = from._direction;
        _directionRecipe    = from._directionRecipe;
        _amount             = from._amount;
        _amountRecipe       = from._amountRecipe;
        _valueDate          = from._valueDate;
    }

    /// <summary>Pins <c>counterparty</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementInstruction WithCounterparty(Lei value) {
        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._counterparty = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>counterparty</c> is drawn from — <c>AnyValidCounterparty()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLei, AnyLei&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlementInstruction WithCounterparty(Func<AnyLei, AnyLei> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._counterpartyRecipe = configure(_counterpartyRecipe);
        copy._counterparty       = copy._counterpartyRecipe;

        return copy;
    }

    /// <summary>Pins <c>direction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementInstruction WithDirection(SettlementDirection value) {
        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._direction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>direction</c> is drawn from — <c>AnyValidDirection()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;SettlementDirection&gt;, AnyEnum&lt;SettlementDirection&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlementInstruction WithDirection(Func<AnyEnum<SettlementDirection>, AnyEnum<SettlementDirection>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._directionRecipe = configure(_directionRecipe);
        copy._direction       = copy._directionRecipe;

        return copy;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementInstruction WithAmount(Money value) {
        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySettlementInstruction WithAmount(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>valueDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySettlementInstruction WithValueDate(DateOnly value) {
        AnySettlementInstruction copy = new AnySettlementInstruction(this);

        copy._valueDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SettlementInstruction" />.</summary>
    public SettlementInstruction Generate() {
        return new SettlementInstruction(_counterparty.Generate(),
                                         _direction.Generate(),
                                         _amount.Generate(),
                                         _valueDate.Generate());
    }

}
