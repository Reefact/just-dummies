// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Molecule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Molecule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMolecule : IAny<Molecule> {

    private IAny<string> _name;

    private AnyPattern _nameRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMolecule()
        : this(name: AnyValidName()) { }

    private static AnyPattern AnyValidName() {
        return Any.StringMatching("[a-z][a-z -]{0,79}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMolecule(AnyPattern name) {
        _name       = name;
        _nameRecipe = name;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMolecule(AnyMolecule from) {
        _name       = from._name;
        _nameRecipe = from._nameRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMolecule WithName(string value) {
        AnyMolecule copy = new AnyMolecule(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMolecule WithName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMolecule copy = new AnyMolecule(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Molecule" />.</summary>
    public Molecule Generate() {
        return new Molecule(_name.Generate());
    }

}
