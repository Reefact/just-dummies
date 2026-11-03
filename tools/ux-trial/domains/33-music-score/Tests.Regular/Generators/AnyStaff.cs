// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Staff --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Staff" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStaff : IAny<Staff> {

    private IAny<string>                 _name;
    private IAny<IReadOnlyList<Measure>> _measures;
    private bool                         _measuresPinned;
    private IAny<int>                    _measureCount = Any.Int32().Between(1, 6);

    private AnyString        _nameRecipe;
    private AnyList<Measure> _measuresRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStaff()
        : this(name:     AnyValidName(),
               measures: AnyValidMeasures()) { }

    private static AnyString AnyValidName() {
        // Letters only: a name with surrounding blanks is refused, and that is a test of its own.
        return Any.String().Alpha().WithLengthBetween(1, Staff.MaxNameLength);
    }

    private static AnyList<Measure> AnyValidMeasures() {
        // Never drawn from: the measures must be numbered 1 to n, so Generate() numbers them itself,
        // unless a test pins them with WithMeasures(value).
        return Any.ListOf(new AnyMeasure()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStaff(AnyString        name,
                     AnyList<Measure> measures) {
        _name           = name;
        _nameRecipe     = name;
        _measures       = measures;
        _measuresRecipe = measures;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStaff(AnyStaff from) {
        _name           = from._name;
        _nameRecipe     = from._nameRecipe;
        _measures       = from._measures;
        _measuresPinned = from._measuresPinned;
        _measureCount   = from._measureCount;
        _measuresRecipe = from._measuresRecipe;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStaff WithName(string value) {
        AnyStaff copy = new AnyStaff(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStaff WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStaff copy = new AnyStaff(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>measures</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStaff WithMeasures(IReadOnlyList<Measure> value) {
        AnyStaff copy = new AnyStaff(this);

        copy._measures       = Any.Fixed(value);
        copy._measuresPinned = true;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Staff" />.</summary>
    public Staff Generate() {
        IReadOnlyList<Measure> measures = _measuresPinned
            ? _measures.Generate()
            : Enumerable.Range(1, _measureCount.Generate()).Select(number => new AnyMeasure().WithNumber(number).Generate()).ToArray();

        return new Staff(_name.Generate(),
                         measures);
    }

}
