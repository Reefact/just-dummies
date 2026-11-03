// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Contributor --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Contributor" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContributor : IAny<Contributor> {

    private IAny<PersonName>      _name;
    private IAny<ContributorRole> _role;

    private AnyPersonName            _nameRecipe;
    private AnyEnum<ContributorRole> _roleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContributor()
        : this(name: AnyValidName(),
               role: AnyValidRole()) { }

    private static AnyPersonName AnyValidName() {
        return new AnyPersonName();
    }

    private static AnyEnum<ContributorRole> AnyValidRole() {
        return Any.Enum<ContributorRole>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyContributor(AnyPersonName            name,
                           AnyEnum<ContributorRole> role) {
        _name       = name;
        _nameRecipe = name;
        _role       = role;
        _roleRecipe = role;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyContributor(AnyContributor from) {
        _name       = from._name;
        _nameRecipe = from._nameRecipe;
        _role       = from._role;
        _roleRecipe = from._roleRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContributor WithName(PersonName value) {
        AnyContributor copy = new AnyContributor(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPersonName, AnyPersonName&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContributor WithName(Func<AnyPersonName, AnyPersonName> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContributor copy = new AnyContributor(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>role</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyContributor WithRole(ContributorRole value) {
        AnyContributor copy = new AnyContributor(this);

        copy._role = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>role</c> is drawn from — <c>AnyValidRole()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ContributorRole&gt;, AnyEnum&lt;ContributorRole&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyContributor WithRole(Func<AnyEnum<ContributorRole>, AnyEnum<ContributorRole>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyContributor copy = new AnyContributor(this);

        copy._roleRecipe = configure(_roleRecipe);
        copy._role       = copy._roleRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Contributor" />.</summary>
    public Contributor Generate() {
        return new Contributor(_name.Generate(),
                               _role.Generate());
    }

}
