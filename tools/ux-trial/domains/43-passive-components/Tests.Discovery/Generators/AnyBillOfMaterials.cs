// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BillOfMaterials --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BillOfMaterials" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBillOfMaterials : IAny<BillOfMaterials> {

    private IAny<string> _boardName;
    private IAny<string> _revision;

    private AnyPattern _boardNameRecipe;
    private AnyPattern _revisionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBillOfMaterials()
        : this(boardName: AnyValidBoardName(),
               revision:  AnyValidRevision()) { }

    private static AnyPattern AnyValidBoardName() {
        // 1 to 64 characters (BillOfMaterials.MaxBoardNameLength), never a leading or trailing space.
        return Any.StringMatching("[A-Za-z0-9]([A-Za-z0-9 ]{0,62}[A-Za-z0-9])?");
    }

    private static AnyPattern AnyValidRevision() {
        return Any.StringMatching("[A-Z]{1,2}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBillOfMaterials(AnyPattern boardName,
                               AnyPattern revision) {
        _boardName       = boardName;
        _boardNameRecipe = boardName;
        _revision        = revision;
        _revisionRecipe  = revision;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBillOfMaterials(AnyBillOfMaterials from) {
        _boardName       = from._boardName;
        _boardNameRecipe = from._boardNameRecipe;
        _revision        = from._revision;
        _revisionRecipe  = from._revisionRecipe;
    }

    /// <summary>Pins <c>boardName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBillOfMaterials WithBoardName(string value) {
        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._boardName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>boardName</c> is drawn from — <c>AnyValidBoardName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBillOfMaterials WithBoardName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._boardNameRecipe = configure(_boardNameRecipe);
        copy._boardName       = copy._boardNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>revision</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBillOfMaterials WithRevision(string value) {
        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._revision = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>revision</c> is drawn from — <c>AnyValidRevision()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBillOfMaterials WithRevision(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._revisionRecipe = configure(_revisionRecipe);
        copy._revision       = copy._revisionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BillOfMaterials" />.</summary>
    public BillOfMaterials Generate() {
        return new BillOfMaterials(_boardName.Generate(),
                                   _revision.Generate());
    }

}
