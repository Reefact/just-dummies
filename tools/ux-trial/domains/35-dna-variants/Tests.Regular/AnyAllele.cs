// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Allele --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using DnaVariants.Domain;
using JustDummies;

namespace DnaVariants.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Allele" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAllele : IAny<Allele> {

    private IAny<string> _bases;

    private AnyString _basesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAllele()
        : this(bases: AnyValidBases()) { }

    private static AnyString AnyValidBases() {
        return Any.String().WithChars("ACGT").WithMaxLength(50);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAllele(AnyString bases) {
        _bases       = bases;
        _basesRecipe = bases;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAllele(AnyAllele from) {
        _bases       = from._bases;
        _basesRecipe = from._basesRecipe;
    }

    /// <summary>Pins <c>bases</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAllele WithBases(string value) {
        AnyAllele copy = new AnyAllele(this);

        copy._bases = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>bases</c> is drawn from — <c>AnyValidBases()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAllele WithBases(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAllele copy = new AnyAllele(this);

        copy._basesRecipe = configure(_basesRecipe);
        copy._bases       = copy._basesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Allele" />.</summary>
    public Allele Generate() {
        return new Allele(_bases.Generate());
    }

}
