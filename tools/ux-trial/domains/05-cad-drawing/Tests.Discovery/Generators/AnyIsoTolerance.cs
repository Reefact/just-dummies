// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IsoTolerance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IsoTolerance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIsoTolerance : IAny<IsoTolerance> {

    private IAny<string> _zone;
    private IAny<int>    _grade;

    private AnyString _zoneRecipe;
    private AnyInt32  _gradeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIsoTolerance()
        : this(zone:  AnyValidZone(),
               grade: AnyValidGrade()) { }

    private static AnyString AnyValidZone() {
        return Any.String().OneOf("H", "JS", "h", "js");
    }

    private static AnyInt32 AnyValidGrade() {
        return Any.Int32().Between(IsoTolerance.FinestGrade, IsoTolerance.CoarsestGrade);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIsoTolerance(AnyString zone,
                            AnyInt32  grade) {
        _zone        = zone;
        _zoneRecipe  = zone;
        _grade       = grade;
        _gradeRecipe = grade;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIsoTolerance(AnyIsoTolerance from) {
        _zone        = from._zone;
        _zoneRecipe  = from._zoneRecipe;
        _grade       = from._grade;
        _gradeRecipe = from._gradeRecipe;
    }

    /// <summary>Pins <c>zone</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIsoTolerance WithZone(string value) {
        AnyIsoTolerance copy = new AnyIsoTolerance(this);

        copy._zone = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>zone</c> is drawn from — <c>AnyValidZone()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIsoTolerance WithZone(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIsoTolerance copy = new AnyIsoTolerance(this);

        copy._zoneRecipe = configure(_zoneRecipe);
        copy._zone       = copy._zoneRecipe;

        return copy;
    }

    /// <summary>Pins <c>grade</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIsoTolerance WithGrade(int value) {
        AnyIsoTolerance copy = new AnyIsoTolerance(this);

        copy._grade = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>grade</c> is drawn from — <c>AnyValidGrade()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIsoTolerance WithGrade(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIsoTolerance copy = new AnyIsoTolerance(this);

        copy._gradeRecipe = configure(_gradeRecipe);
        copy._grade       = copy._gradeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="IsoTolerance" />.</summary>
    public IsoTolerance Generate() {
        return new IsoTolerance(_zone.Generate(),
                                _grade.Generate());
    }

}
