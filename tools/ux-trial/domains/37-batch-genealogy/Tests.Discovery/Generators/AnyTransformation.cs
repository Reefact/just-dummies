// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Transformation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transformation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransformation : IAny<Transformation> {

    private IAny<string>                     _id;
    private IAny<TransformationKind>         _kind;
    private IAny<IReadOnlyList<LotQuantity>> _inputs;
    private IAny<IReadOnlyList<LotQuantity>> _outputs;
    private IAny<DateOnly>                   _performedOn;

    private AnyPattern                   _idRecipe;
    private AnyEnum<TransformationKind> _kindRecipe;
    private AnyList<LotQuantity>        _inputsRecipe;
    private AnyList<LotQuantity>        _outputsRecipe;
    private AnyDateOnly                 _performedOnRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransformation()
        : this(id:          AnyValidId(),
               kind:        AnyValidKind(),
               inputs:      AnyValidInputs(),
               outputs:     AnyValidOutputs(),
               performedOn: AnyValidPerformedOn()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching("TRF-[0-9]{6}");
    }

    private static AnyEnum<TransformationKind> AnyValidKind() {
        // The base is a Process: one input, one output (the arity of the default inputs and outputs below).
        return Any.Enum<TransformationKind>().OneOf(TransformationKind.Process);
    }

    private static AnyList<LotQuantity> AnyValidInputs() {
        // All in grams, and more than any output below can weigh, so the output never exceeds the input.
        return Any.ListOf(new AnyLotQuantity().WithQuantity(q => q.WithUnit(QuantityUnit.Gram).WithAmount(a => a.Between(1_000m, 5_000m))))
                  .WithCount(1);
    }

    private static AnyList<LotQuantity> AnyValidOutputs() {
        return Any.ListOf(new AnyLotQuantity().WithQuantity(q => q.WithUnit(QuantityUnit.Gram).WithAmount(a => a.Between(1m, 999m))))
                  .WithCount(1);
    }

    private static AnyDateOnly AnyValidPerformedOn() {
        return Any.DateOnly();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTransformation(AnyPattern                   id,
                              AnyEnum<TransformationKind> kind,
                              AnyList<LotQuantity>        inputs,
                              AnyList<LotQuantity>        outputs,
                              AnyDateOnly                 performedOn) {
        _id                = id;
        _idRecipe          = id;
        _kind              = kind;
        _kindRecipe        = kind;
        _inputs            = inputs;
        _inputsRecipe      = inputs;
        _outputs           = outputs;
        _outputsRecipe     = outputs;
        _performedOn       = performedOn;
        _performedOnRecipe = performedOn;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTransformation(AnyTransformation from) {
        _id                = from._id;
        _idRecipe          = from._idRecipe;
        _kind              = from._kind;
        _kindRecipe        = from._kindRecipe;
        _inputs            = from._inputs;
        _inputsRecipe      = from._inputsRecipe;
        _outputs           = from._outputs;
        _outputsRecipe     = from._outputsRecipe;
        _performedOn       = from._performedOn;
        _performedOnRecipe = from._performedOnRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformation WithId(string value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformation WithId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformation WithKind(TransformationKind value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TransformationKind&gt;, AnyEnum&lt;TransformationKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformation WithKind(Func<AnyEnum<TransformationKind>, AnyEnum<TransformationKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>inputs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformation WithInputs(IReadOnlyList<LotQuantity> value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._inputs = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>inputs</c> is drawn from — <c>AnyValidInputs()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;LotQuantity&gt;, AnyList&lt;LotQuantity&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformation WithInputs(Func<AnyList<LotQuantity>, AnyList<LotQuantity>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._inputsRecipe = configure(_inputsRecipe);
        copy._inputs       = copy._inputsRecipe;

        return copy;
    }

    /// <summary>Pins <c>outputs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformation WithOutputs(IReadOnlyList<LotQuantity> value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._outputs = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>outputs</c> is drawn from — <c>AnyValidOutputs()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;LotQuantity&gt;, AnyList&lt;LotQuantity&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformation WithOutputs(Func<AnyList<LotQuantity>, AnyList<LotQuantity>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._outputsRecipe = configure(_outputsRecipe);
        copy._outputs       = copy._outputsRecipe;

        return copy;
    }

    /// <summary>Pins <c>performedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTransformation WithPerformedOn(DateOnly value) {
        AnyTransformation copy = new AnyTransformation(this);

        copy._performedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>performedOn</c> is drawn from — <c>AnyValidPerformedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTransformation WithPerformedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTransformation copy = new AnyTransformation(this);

        copy._performedOnRecipe = configure(_performedOnRecipe);
        copy._performedOn       = copy._performedOnRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Transformation" />.</summary>
    public Transformation Generate() {
        return new Transformation(_id.Generate(),
                                  _kind.Generate(),
                                  _inputs.Generate(),
                                  _outputs.Generate(),
                                  _performedOn.Generate());
    }

}
