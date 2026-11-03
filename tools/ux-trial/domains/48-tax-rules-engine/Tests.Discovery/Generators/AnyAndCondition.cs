// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AndCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System.Collections.Generic;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AndCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAndCondition : IAny<AndCondition> {

    private IAny<IEnumerable<ICondition>> _operands;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAndCondition()
        : this(operands: AnyValidOperands()) { }

    private static IAny<IEnumerable<ICondition>> AnyValidOperands() {
        return Any.ListOf(new AnyLeafCondition()).WithCountBetween(2, 4).As(l => (IEnumerable<ICondition>)l);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAndCondition(IAny<IEnumerable<ICondition>> operands) {
        _operands = operands;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAndCondition(AnyAndCondition from) {
        _operands = from._operands;
    }

    /// <summary>Pins <c>operands</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyAndCondition WithOperands(IEnumerable<ICondition> value) {
        AnyAndCondition copy = new AnyAndCondition(this);

        copy._operands = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AndCondition" />.</summary>
    public AndCondition Generate() {
        return new AndCondition(_operands.Generate());
    }

}
