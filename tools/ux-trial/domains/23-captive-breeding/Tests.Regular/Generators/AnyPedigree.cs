// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pedigree --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Pedigree" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPedigree : IAny<Pedigree> {

    private IAny<string> _programmeName;
    private IAny<string> _species;

    private AnyString _programmeNameRecipe;
    private AnyString _speciesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPedigree()
        : this(programmeName: AnyValidProgrammeName(),
               species:       AnyValidSpecies()) { }

    private static AnyString AnyValidProgrammeName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidSpecies() {
        return Any.String().NotBlank();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPedigree(AnyString programmeName,
                        AnyString species) {
        _programmeName       = programmeName;
        _programmeNameRecipe = programmeName;
        _species             = species;
        _speciesRecipe       = species;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPedigree(AnyPedigree from) {
        _programmeName       = from._programmeName;
        _programmeNameRecipe = from._programmeNameRecipe;
        _species             = from._species;
        _speciesRecipe       = from._speciesRecipe;
    }

    /// <summary>Pins <c>programmeName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPedigree WithProgrammeName(string value) {
        AnyPedigree copy = new AnyPedigree(this);

        copy._programmeName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>programmeName</c> is drawn from — <c>AnyValidProgrammeName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPedigree WithProgrammeName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPedigree copy = new AnyPedigree(this);

        copy._programmeNameRecipe = configure(_programmeNameRecipe);
        copy._programmeName       = copy._programmeNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>species</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPedigree WithSpecies(string value) {
        AnyPedigree copy = new AnyPedigree(this);

        copy._species = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>species</c> is drawn from — <c>AnyValidSpecies()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPedigree WithSpecies(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPedigree copy = new AnyPedigree(this);

        copy._speciesRecipe = configure(_speciesRecipe);
        copy._species       = copy._speciesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Pedigree" />.</summary>
    public Pedigree Generate() {
        return new Pedigree(_programmeName.Generate(),
                            _species.Generate());
    }

}
