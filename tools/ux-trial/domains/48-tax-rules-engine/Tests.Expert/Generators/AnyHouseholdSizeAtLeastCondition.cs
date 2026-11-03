// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate HouseholdSizeAtLeastCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="HouseholdSizeAtLeastCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHouseholdSizeAtLeastCondition : IAny<HouseholdSizeAtLeastCondition> {

    private IAny<int> _members;

    private AnyInt32 _membersRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHouseholdSizeAtLeastCondition()
        : this(members: AnyValidMembers()) { }

    private static AnyInt32 AnyValidMembers() {
        return Any.Int32().Between(2, Taxpayer.MaxHouseholdSize);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyHouseholdSizeAtLeastCondition(AnyInt32 members) {
        _members       = members;
        _membersRecipe = members;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyHouseholdSizeAtLeastCondition(AnyHouseholdSizeAtLeastCondition from) {
        _members       = from._members;
        _membersRecipe = from._membersRecipe;
    }

    /// <summary>Pins <c>members</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHouseholdSizeAtLeastCondition WithMembers(int value) {
        AnyHouseholdSizeAtLeastCondition copy = new AnyHouseholdSizeAtLeastCondition(this);

        copy._members = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>members</c> is drawn from — <c>AnyValidMembers()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHouseholdSizeAtLeastCondition WithMembers(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHouseholdSizeAtLeastCondition copy = new AnyHouseholdSizeAtLeastCondition(this);

        copy._membersRecipe = configure(_membersRecipe);
        copy._members       = copy._membersRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="HouseholdSizeAtLeastCondition" />.</summary>
    public HouseholdSizeAtLeastCondition Generate() {
        return new HouseholdSizeAtLeastCondition(_members.Generate());
    }

}
