// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Drawing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<DrawingReference>         _reference;
    private readonly IAny<string>                   _title;
    private readonly IAny<DrawingScale>             _scale;
    private readonly IAny<ProjectionMethod>         _projection;
    private readonly IAny<DateOnly>                 _issuedOn;
    private readonly IAny<IReadOnlyList<Dimension>> _dimensions;

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
        return Any.DateOnly().AfterOrEqualTo(Drawing.EarliestIssue);
    }

    private static AnyList<Dimension> AnyValidDimensions() {
        return Any.ListOf(new AnyDimension());
    }

    private AnyDrawing(IAny<DrawingReference>         reference,
                       IAny<string>                   title,
                       IAny<DrawingScale>             scale,
                       IAny<ProjectionMethod>         projection,
                       IAny<DateOnly>                 issuedOn,
                       IAny<IReadOnlyList<Dimension>> dimensions) {
        _reference  = reference;
        _title      = title;
        _scale      = scale;
        _projection = projection;
        _issuedOn   = issuedOn;
        _dimensions = dimensions;
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyDrawing WithReference(DrawingReference value) {
        return new AnyDrawing(new FixedValue<DrawingReference>(value), _title, _scale, _projection, _issuedOn, _dimensions);
    }

    /// <summary>Draws <c>reference</c> from <c>AnyValidReference()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithReference(Func<AnyDrawingReference, AnyDrawingReference> configure) {
        return new AnyDrawing(configure(AnyValidReference()), _title, _scale, _projection, _issuedOn, _dimensions);
    }

    /// <summary>Pins <c>title</c> to a fixed value.</summary>
    public AnyDrawing WithTitle(string value) {
        return new AnyDrawing(_reference, new FixedValue<string>(value), _scale, _projection, _issuedOn, _dimensions);
    }

    /// <summary>Draws <c>title</c> from <c>AnyValidTitle()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithTitle(Func<AnyString, AnyString> configure) {
        return new AnyDrawing(_reference, configure(AnyValidTitle()), _scale, _projection, _issuedOn, _dimensions);
    }

    /// <summary>Pins <c>scale</c> to a fixed value.</summary>
    public AnyDrawing WithScale(DrawingScale value) {
        return new AnyDrawing(_reference, _title, new FixedValue<DrawingScale>(value), _projection, _issuedOn, _dimensions);
    }

    /// <summary>Draws <c>scale</c> from <c>AnyValidScale()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithScale(Func<AnyDrawingScale, AnyDrawingScale> configure) {
        return new AnyDrawing(_reference, _title, configure(AnyValidScale()), _projection, _issuedOn, _dimensions);
    }

    /// <summary>Pins <c>projection</c> to a fixed value.</summary>
    public AnyDrawing WithProjection(ProjectionMethod value) {
        return new AnyDrawing(_reference, _title, _scale, new FixedValue<ProjectionMethod>(value), _issuedOn, _dimensions);
    }

    /// <summary>Draws <c>projection</c> from <c>AnyValidProjection()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithProjection(Func<AnyEnum<ProjectionMethod>, AnyEnum<ProjectionMethod>> configure) {
        return new AnyDrawing(_reference, _title, _scale, configure(AnyValidProjection()), _issuedOn, _dimensions);
    }

    /// <summary>Pins <c>issuedOn</c> to a fixed value.</summary>
    public AnyDrawing WithIssuedOn(DateOnly value) {
        return new AnyDrawing(_reference, _title, _scale, _projection, new FixedValue<DateOnly>(value), _dimensions);
    }

    /// <summary>Draws <c>issuedOn</c> from <c>AnyValidIssuedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyDrawing(_reference, _title, _scale, _projection, configure(AnyValidIssuedOn()), _dimensions);
    }

    /// <summary>Pins <c>dimensions</c> to a fixed value.</summary>
    public AnyDrawing WithDimensions(IReadOnlyList<Dimension> value) {
        return new AnyDrawing(_reference, _title, _scale, _projection, _issuedOn, new FixedValue<IReadOnlyList<Dimension>>(value));
    }

    /// <summary>Draws <c>dimensions</c> from <c>AnyValidDimensions()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDrawing WithDimensions(Func<AnyList<Dimension>, AnyList<Dimension>> configure) {
        return new AnyDrawing(_reference, _title, _scale, _projection, _issuedOn, configure(AnyValidDimensions()));
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

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
