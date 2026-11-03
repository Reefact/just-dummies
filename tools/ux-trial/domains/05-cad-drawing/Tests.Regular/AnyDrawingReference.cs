// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DrawingReference --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DrawingReference" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDrawingReference : IAny<DrawingReference> {

    private IAny<string> _number;
    private IAny<char>   _revision;
    private IAny<int>    _sheet;

    private AnyPattern _numberRecipe;
    private AnyChar   _revisionRecipe;
    private AnyInt32  _sheetRecipe;

    // A-Y without the letters ASME Y14.35 forbids (I, O, Q, S, X).
    private static readonly char[] AllowedRevisions = "ABCDEFGHJKLMNPRTUVWY".ToCharArray();

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDrawingReference()
        : this(number:   AnyValidNumber(),
               revision: AnyValidRevision(),
               sheet:    AnyValidSheet()) { }

    private static AnyPattern AnyValidNumber() {
        return Any.StringMatching("(MEC|ELE|HYD)-[0-9]{5}");
    }

    private static AnyChar AnyValidRevision() {
        return Any.Char().OneOf(AllowedRevisions);
    }

    private static AnyInt32 AnyValidSheet() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDrawingReference(AnyPattern number,
                                AnyChar   revision,
                                AnyInt32  sheet) {
        _number         = number;
        _numberRecipe   = number;
        _revision       = revision;
        _revisionRecipe = revision;
        _sheet          = sheet;
        _sheetRecipe    = sheet;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDrawingReference(AnyDrawingReference from) {
        _number         = from._number;
        _numberRecipe   = from._numberRecipe;
        _revision       = from._revision;
        _revisionRecipe = from._revisionRecipe;
        _sheet          = from._sheet;
        _sheetRecipe    = from._sheetRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawingReference WithNumber(string value) {
        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawingReference WithNumber(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>revision</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawingReference WithRevision(char value) {
        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._revision = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>revision</c> is drawn from — <c>AnyValidRevision()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyChar, AnyChar&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawingReference WithRevision(Func<AnyChar, AnyChar> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._revisionRecipe = configure(_revisionRecipe);
        copy._revision       = copy._revisionRecipe;

        return copy;
    }

    /// <summary>Pins <c>sheet</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawingReference WithSheet(int value) {
        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._sheet = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sheet</c> is drawn from — <c>AnyValidSheet()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawingReference WithSheet(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawingReference copy = new AnyDrawingReference(this);

        copy._sheetRecipe = configure(_sheetRecipe);
        copy._sheet       = copy._sheetRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DrawingReference" />.</summary>
    public DrawingReference Generate() {
        return new DrawingReference(_number.Generate(),
                                    _revision.Generate(),
                                    _sheet.Generate());
    }

}
