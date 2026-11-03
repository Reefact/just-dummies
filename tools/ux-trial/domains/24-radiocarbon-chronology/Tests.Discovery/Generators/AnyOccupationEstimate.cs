// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OccupationEstimate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OccupationEstimate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOccupationEstimate : IAny<OccupationEstimate> {

    private IAny<string>         _layerCode;
    private IAny<CalibratedDate> _date;

    private AnyString         _layerCodeRecipe;
    private AnyCalibratedDate _dateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOccupationEstimate()
        : this(layerCode: AnyValidLayerCode(),
               date:      AnyValidDate()) { }

    private static AnyString AnyValidLayerCode() {
        return Any.String().NotBlank();
    }

    private static AnyCalibratedDate AnyValidDate() {
        return new AnyCalibratedDate();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOccupationEstimate(AnyString         layerCode,
                                  AnyCalibratedDate date) {
        _layerCode       = layerCode;
        _layerCodeRecipe = layerCode;
        _date            = date;
        _dateRecipe      = date;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOccupationEstimate(AnyOccupationEstimate from) {
        _layerCode       = from._layerCode;
        _layerCodeRecipe = from._layerCodeRecipe;
        _date            = from._date;
        _dateRecipe      = from._dateRecipe;
    }

    /// <summary>Pins <c>layerCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOccupationEstimate WithLayerCode(string value) {
        AnyOccupationEstimate copy = new AnyOccupationEstimate(this);

        copy._layerCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>layerCode</c> is drawn from — <c>AnyValidLayerCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOccupationEstimate WithLayerCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOccupationEstimate copy = new AnyOccupationEstimate(this);

        copy._layerCodeRecipe = configure(_layerCodeRecipe);
        copy._layerCode       = copy._layerCodeRecipe;

        return copy;
    }

    /// <summary>Pins <c>date</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOccupationEstimate WithDate(CalibratedDate value) {
        AnyOccupationEstimate copy = new AnyOccupationEstimate(this);

        copy._date = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>date</c> is drawn from — <c>AnyValidDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCalibratedDate, AnyCalibratedDate&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOccupationEstimate WithDate(Func<AnyCalibratedDate, AnyCalibratedDate> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOccupationEstimate copy = new AnyOccupationEstimate(this);

        copy._dateRecipe = configure(_dateRecipe);
        copy._date       = copy._dateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="OccupationEstimate" />.</summary>
    public OccupationEstimate Generate() {
        return new OccupationEstimate(_layerCode.Generate(),
                                      _date.Generate());
    }

}
