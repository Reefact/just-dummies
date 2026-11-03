// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NotCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="NotCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNotCondition : IAny<NotCondition> {

    private IAny<ICondition> _operand;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNotCondition()
        : this(operand: AnyValidOperand()) { }

    private static IAny<ICondition> AnyValidOperand() {
        // A negation of a negation is refused (Not.cs line 9): the operand is a leaf.
        return AnyICondition.Leaf();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNotCondition(IAny<ICondition> operand) {
        _operand = operand;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNotCondition(AnyNotCondition from) {
        _operand = from._operand;
    }

    /// <summary>Pins <c>operand</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyNotCondition WithOperand(ICondition value) {
        AnyNotCondition copy = new AnyNotCondition(this);

        copy._operand = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NotCondition" />.</summary>
    public NotCondition Generate() {
        return new NotCondition(_operand.Generate());
    }

}
