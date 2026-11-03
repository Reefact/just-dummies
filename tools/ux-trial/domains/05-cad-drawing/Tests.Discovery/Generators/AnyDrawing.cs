// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Drawing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Drawing" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDrawing : IAny<Drawing> {

    private IAny<DrawingReference>         _reference;
    private IAny<string>                   _title;
    private IAny<DrawingScale>             _scale;
    private IAny<ProjectionMethod>         _projection;
    private IAny<DateOnly>                 _issuedOn;
    private IAny<IReadOnlyList<Dimension>> _dimensions;

    private AnyDrawingReference       _referenceRecipe;
    private AnyString                 _titleRecipe;
    private AnyDrawingScale           _scaleRecipe;
    private AnyEnum<ProjectionMethod> _projectionRecipe;
    private AnyDateOnly               _issuedOnRecipe;
    private AnyList<Dimension>        _dimensionsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDrawing()
        : this(reference:  AnyValidReference(),
               title:      AnyValidTitle(),
               scale:      AnyValidScale(),
               projection: AnyValidProjection(),
               issuedOn:   AnyValidIssuedOn(),
               dimensions: AnyValidDimensions()) { }

    private static AnyDrawingReference AnyValidReference() {
        return new AnyDrawingReference();
    }

    private static AnyString AnyValidTitle() {
        return Any.String().NotBlank();
    }

    private static AnyDrawingScale AnyValidScale() {
        return new AnyDrawingScale();
    }

    private static AnyEnum<ProjectionMethod> AnyValidProjection() {
        return Any.Enum<ProjectionMethod>();
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly().Between(Drawing.EarliestIssue, new DateOnly(2100, 12, 31));
    }

    private static AnyList<Dimension> AnyValidDimensions() {
        return Any.ListOf(new AnyDimension());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDrawing(AnyDrawingReference       reference,
                       AnyString                 title,
                       AnyDrawingScale           scale,
                       AnyEnum<ProjectionMethod> projection,
                       AnyDateOnly               issuedOn,
                       AnyList<Dimension>        dimensions) {
        _reference        = reference;
        _referenceRecipe  = reference;
        _title            = title;
        _titleRecipe      = title;
        _scale            = scale;
        _scaleRecipe      = scale;
        _projection       = projection;
        _projectionRecipe = projection;
        _issuedOn         = issuedOn;
        _issuedOnRecipe   = issuedOn;
        _dimensions       = dimensions;
        _dimensionsRecipe = dimensions;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDrawing(AnyDrawing from) {
        _reference        = from._reference;
        _referenceRecipe  = from._referenceRecipe;
        _title            = from._title;
        _titleRecipe      = from._titleRecipe;
        _scale            = from._scale;
        _scaleRecipe      = from._scaleRecipe;
        _projection       = from._projection;
        _projectionRecipe = from._projectionRecipe;
        _issuedOn         = from._issuedOn;
        _issuedOnRecipe   = from._issuedOnRecipe;
        _dimensions       = from._dimensions;
        _dimensionsRecipe = from._dimensionsRecipe;
    }

    /// <summary>Pins <c>reference</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithReference(DrawingReference value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._reference = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reference</c> is drawn from — <c>AnyValidReference()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDrawingReference, AnyDrawingReference&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithReference(Func<AnyDrawingReference, AnyDrawingReference> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._referenceRecipe = configure(_referenceRecipe);
        copy._reference       = copy._referenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithTitle(string value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._title = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;

        return copy;
    }

    /// <summary>Pins <c>scale</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithScale(DrawingScale value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._scale = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>scale</c> is drawn from — <c>AnyValidScale()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDrawingScale, AnyDrawingScale&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithScale(Func<AnyDrawingScale, AnyDrawingScale> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._scaleRecipe = configure(_scaleRecipe);
        copy._scale       = copy._scaleRecipe;

        return copy;
    }

    /// <summary>Pins <c>projection</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithProjection(ProjectionMethod value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._projection = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>projection</c> is drawn from — <c>AnyValidProjection()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ProjectionMethod&gt;, AnyEnum&lt;ProjectionMethod&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithProjection(Func<AnyEnum<ProjectionMethod>, AnyEnum<ProjectionMethod>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._projectionRecipe = configure(_projectionRecipe);
        copy._projection       = copy._projectionRecipe;

        return copy;
    }

    /// <summary>Pins <c>issuedOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithIssuedOn(DateOnly value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._issuedOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>issuedOn</c> is drawn from — <c>AnyValidIssuedOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._issuedOnRecipe = configure(_issuedOnRecipe);
        copy._issuedOn       = copy._issuedOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>dimensions</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawing WithDimensions(IReadOnlyList<Dimension> value) {
        AnyDrawing copy = new AnyDrawing(this);

        copy._dimensions = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dimensions</c> is drawn from — <c>AnyValidDimensions()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Dimension&gt;, AnyList&lt;Dimension&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawing WithDimensions(Func<AnyList<Dimension>, AnyList<Dimension>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawing copy = new AnyDrawing(this);

        copy._dimensionsRecipe = configure(_dimensionsRecipe);
        copy._dimensions       = copy._dimensionsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Drawing" />.</summary>
    public Drawing Generate() {
        return new Drawing(_reference.Generate(),
                           _title.Generate(),
                           _scale.Generate(),
                           _projection.Generate(),
                           _issuedOn.Generate(),
                           _dimensions.Generate());
    }

}
