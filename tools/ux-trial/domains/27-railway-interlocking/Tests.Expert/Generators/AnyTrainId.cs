// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrainId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="TrainId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrainId : IAny<TrainId> {

    private IAny<string> _headcode;

    private AnyPattern _headcodeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrainId()
        : this(headcode: AnyValidHeadcode()) { }

    private static AnyPattern AnyValidHeadcode() {
        return Any.StringMatching("[0-9][A-Z][0-9]{2}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTrainId(AnyPattern headcode) {
        _headcode       = headcode;
        _headcodeRecipe = headcode;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTrainId(AnyTrainId from) {
        _headcode       = from._headcode;
        _headcodeRecipe = from._headcodeRecipe;
    }

    /// <summary>Pins <c>headcode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTrainId WithHeadcode(string value) {
        AnyTrainId copy = new AnyTrainId(this);

        copy._headcode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>headcode</c> is drawn from — <c>AnyValidHeadcode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTrainId WithHeadcode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTrainId copy = new AnyTrainId(this);

        copy._headcodeRecipe = configure(_headcodeRecipe);
        copy._headcode       = copy._headcodeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TrainId" />.</summary>
    public TrainId Generate() {
        return new TrainId(_headcode.Generate());
    }

}
